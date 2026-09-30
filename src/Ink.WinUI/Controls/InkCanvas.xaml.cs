using System.Numerics;
using InkControl.Input;
using InkControl.Models;
using inkapp.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using InkDrawingAttributes = Windows.UI.Input.Inking.InkDrawingAttributes;
using NativeInkPresenter = Microsoft.UI.Xaml.Controls.InkPresenter;
using NativeInkStroke = Windows.UI.Input.Inking.InkStroke;
using NativeProcessingMode = Microsoft.UI.Xaml.Controls.InkInputProcessingMode;
using PointerDeviceType = Microsoft.UI.Input.PointerDeviceType;

namespace InkControl.Controls;

/// <summary>
/// Infinite world-space canvas backed entirely by Windows App SDK 2.5.4 native ink.
/// The OS owns both wet and dry ink; only the paper pattern is drawn by the app.
/// </summary>
public sealed partial class InkCanvas : UserControl, IDisposable
{
    private sealed record PresentedStroke(Stroke Model, NativeInkStroke Native, Matrix3x2 SourceToWorld);

    private readonly Dictionary<uint, PresentedStroke> _presented = [];
    private readonly HashSet<uint> _ignoredStrokes = [];
    private readonly HashSet<uint> _inkingPointers = [];
    private readonly Dictionary<uint, Vector2> _touches = [];
    private readonly NativePenHaptics _haptics;
    private readonly NativeInkPresenter _presenter;
    private StrokeCollection _strokes = new();
    private ViewportState _viewport = ViewportState.Default;
    private ViewportState _renderedViewport = ViewportState.Default;
    private ViewportState _captureViewport = ViewportState.Default;
    private uint? _panPointer;
    private Vector2 _panPosition;
    private bool _awaitingCollectedStroke;
    private bool _discardCurrentStroke;
    private bool _frameQueued;
    private bool _updatingProperties;
    private bool _updatingToolbar;
    private bool _initialized;
    private bool _disposed;

    public static readonly DependencyProperty PenColorProperty =
        DependencyProperty.Register(nameof(PenColor), typeof(Color), typeof(InkCanvas),
            new PropertyMetadata(Microsoft.UI.Colors.Black, OnPenPropertyChanged));

    public Color PenColor
    {
        get => (Color)GetValue(PenColorProperty);
        set => SetValue(PenColorProperty, value);
    }

    public static readonly DependencyProperty PenThicknessProperty =
        DependencyProperty.Register(nameof(PenThickness), typeof(double), typeof(InkCanvas),
            new PropertyMetadata(2.0, OnPenPropertyChanged));

    /// <summary>Pen width in world-space DIPs, before zoom.</summary>
    public double PenThickness
    {
        get => (double)GetValue(PenThicknessProperty);
        set => SetValue(PenThicknessProperty, value);
    }

    public static readonly DependencyProperty ToolModeProperty =
        DependencyProperty.Register(nameof(ToolMode), typeof(ToolMode), typeof(InkCanvas),
            new PropertyMetadata(ToolMode.Pen, OnToolModeChanged));

    public ToolMode ToolMode
    {
        get => (ToolMode)GetValue(ToolModeProperty);
        set => SetValue(ToolModeProperty, value);
    }

    public static readonly DependencyProperty IsMouseInkingEnabledProperty =
        DependencyProperty.Register(nameof(IsMouseInkingEnabled), typeof(bool), typeof(InkCanvas),
            new PropertyMetadata(false, OnInputDevicesChanged));

    /// <summary>Enables left-mouse drawing. Touch always navigates rather than inks.</summary>
    public bool IsMouseInkingEnabled
    {
        get => (bool)GetValue(IsMouseInkingEnabledProperty);
        set => SetValue(IsMouseInkingEnabledProperty, value);
    }

    public static readonly DependencyProperty BgTypeProperty =
        DependencyProperty.Register(nameof(BgType), typeof(BackgroundType), typeof(InkCanvas),
            new PropertyMetadata(BackgroundType.Blank, OnBackgroundChanged));

    public BackgroundType BgType
    {
        get => (BackgroundType)GetValue(BgTypeProperty);
        set => SetValue(BgTypeProperty, value);
    }

    public static readonly DependencyProperty BgSpacingProperty =
        DependencyProperty.Register(nameof(BgSpacing), typeof(double), typeof(InkCanvas),
            new PropertyMetadata(24.0, OnBackgroundChanged));

    public double BgSpacing
    {
        get => (double)GetValue(BgSpacingProperty);
        set => SetValue(BgSpacingProperty, value);
    }

    public static readonly DependencyProperty BgColorArgbProperty =
        DependencyProperty.Register(nameof(BgColorArgb), typeof(int), typeof(InkCanvas),
            new PropertyMetadata(unchecked((int)0x20000000), OnBackgroundChanged));

    public int BgColorArgb
    {
        get => (int)GetValue(BgColorArgbProperty);
        set => SetValue(BgColorArgbProperty, value);
    }

    public static readonly DependencyProperty PanXProperty =
        DependencyProperty.Register(nameof(PanX), typeof(double), typeof(InkCanvas),
            new PropertyMetadata(0.0, OnViewportChanged));

    public double PanX
    {
        get => (double)GetValue(PanXProperty);
        set => SetValue(PanXProperty, value);
    }

    public static readonly DependencyProperty PanYProperty =
        DependencyProperty.Register(nameof(PanY), typeof(double), typeof(InkCanvas),
            new PropertyMetadata(0.0, OnViewportChanged));

    public double PanY
    {
        get => (double)GetValue(PanYProperty);
        set => SetValue(PanYProperty, value);
    }

    public static readonly DependencyProperty ZoomProperty =
        DependencyProperty.Register(nameof(Zoom), typeof(double), typeof(InkCanvas),
            new PropertyMetadata(1.0, OnViewportChanged));

    public double Zoom
    {
        get => (double)GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    public event EventHandler? StrokesChanged;
    public event EventHandler<StrokeCompletedEventArgs>? StrokeCompleted;
    public event EventHandler<ViewportChangedEventArgs>? ViewportChanged;

    public InkCanvas() : this(null) { }

    public InkCanvas(ILoggerFactory? loggerFactory)
    {
        _haptics = new NativePenHaptics(loggerFactory?.CreateLogger<InkCanvas>());
        InitializeComponent();
        _presenter = NativeCanvas.InkPresenter;
        PenButton.Palette = new List<Brush>
        {
            new SolidColorBrush(Microsoft.UI.Colors.Black),
            new SolidColorBrush(Microsoft.UI.Colors.Blue),
            new SolidColorBrush(Microsoft.UI.Colors.Red),
            new SolidColorBrush(Microsoft.UI.Colors.Green),
            new SolidColorBrush(Microsoft.UI.Colors.Purple),
            new SolidColorBrush(Microsoft.UI.Colors.Orange)
        };
        _presenter.InputProcessingConfiguration.RightDragAction = InkInputRightDragAction.LeaveUnprocessed;
        _presenter.InputConfiguration.IsEraserInputEnabled = true;
        _presenter.InputConfiguration.IsPrimaryBarrelButtonInputEnabled = true;
        _presenter.HighContrastAdjustment = InkHighContrastAdjustment.UseSystemColorsWhenNecessary;
        _presenter.StrokesCollected += OnStrokesCollected;
        _presenter.StrokesErased += OnStrokesErased;
        _presenter.StrokeInput.StrokeStarted += OnStrokeStarted;
        _presenter.StrokeInput.StrokeEnded += OnStrokeEnded;
        _presenter.StrokeInput.StrokeCanceled += OnStrokeCanceled;
        _presenter.UnprocessedInput.PointerPressed += OnUnprocessedPressed;
        _presenter.UnprocessedInput.PointerMoved += OnUnprocessedMoved;
        _presenter.UnprocessedInput.PointerReleased += OnUnprocessedReleased;
        _presenter.UnprocessedInput.PointerLost += OnUnprocessedReleased;

        Toolbar.ActiveToolChanged += OnToolbarToolChanged;
        Toolbar.InkDrawingAttributesChanged += OnToolbarAttributesChanged;
        Toolbar.EraseAllClicked += OnToolbarEraseAll;
        _strokes.StrokesChanged += OnStrokesCollectionChanged;
        ViewportGrid.SizeChanged += OnViewportSizeChanged;
        ViewportGrid.AddHandler(PointerWheelChangedEvent, new PointerEventHandler(OnPointerWheelChanged), true);
        ViewportGrid.AddHandler(PointerPressedEvent, new PointerEventHandler(OnTouchPressed), true);
        ViewportGrid.AddHandler(PointerMovedEvent, new PointerEventHandler(OnTouchMoved), true);
        ViewportGrid.AddHandler(PointerReleasedEvent, new PointerEventHandler(OnTouchReleased), true);
        ViewportGrid.AddHandler(PointerCanceledEvent, new PointerEventHandler(OnTouchReleased), true);
        ViewportGrid.AddHandler(PointerCaptureLostEvent, new PointerEventHandler(OnTouchReleased), true);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        _initialized = true;
        UpdateInputDevices();
        UpdateToolbar();
        UpdateDrawingAttributes();
    }

    private bool IsCollecting => _inkingPointers.Count != 0 || _awaitingCollectedStroke;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _presenter.IsInputEnabled = !_disposed;
        RequestViewUpdate();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        CancelQueuedFrame();
        _discardCurrentStroke = IsCollecting;
        _inkingPointers.Clear();
        _awaitingCollectedStroke = false;
        _touches.Clear();
        _panPointer = null;
        _haptics.Stop();
        // Unloading is not disposal: the native control supports reparenting/reloading.
        _presenter.IsInputEnabled = false;
    }

    private void OnViewportSizeChanged(object sender, SizeChangedEventArgs e)
    {
        ViewportClip.Rect = new Rect(0, 0, Math.Max(0, e.NewSize.Width), Math.Max(0, e.NewSize.Height));
        RequestViewUpdate();
    }

    private void RequestViewUpdate()
    {
        if (!_initialized || _disposed || !IsLoaded || _frameQueued || IsCollecting)
            return;

        _frameQueued = true;
        CompositionTarget.Rendering += OnRendering;
    }

    private void CancelQueuedFrame()
    {
        if (!_frameQueued) return;
        CompositionTarget.Rendering -= OnRendering;
        _frameQueued = false;
    }

    private void OnRendering(object? sender, object e)
    {
        CancelQueuedFrame();
        if (_disposed || IsCollecting || !IsLoaded) return;

        SynchronizeNativeStrokes();
        foreach (var entry in _presented.Values)
            NativeStrokeAdapter.Project(entry.Native, entry.SourceToWorld, entry.Model, _viewport);

        var viewportChanged = _renderedViewport != _viewport;
        _renderedViewport = _viewport;
        UpdateDrawingAttributes();
        DrawBackground();
        if (viewportChanged)
            ViewportChanged?.Invoke(this, new ViewportChangedEventArgs
            {
                PanX = _viewport.Pan.X, PanY = _viewport.Pan.Y, Zoom = _viewport.Zoom
            });
    }

    private void SynchronizeNativeStrokes()
    {
        var models = _strokes.Strokes.ToDictionary(s => s.Id);
        var removed = _presented.Values
            .Where(p => !models.TryGetValue(p.Model.Id, out var model) || !ReferenceEquals(model, p.Model))
            .ToArray();
        foreach (var entry in removed)
        {
            _presented.Remove(entry.Native.Id);
            entry.Native.Selected = true;
        }
        if (removed.Length != 0)
            _presenter.StrokeContainer.DeleteSelected();

        var existing = _presented.Values.Select(p => p.Model.Id).ToHashSet();
        var added = new List<NativeInkStroke>();
        foreach (var model in models.Values)
        {
            if (existing.Contains(model.Id) || model.Points.Count == 0) continue;
            var native = NativeStrokeAdapter.CreateNative(model);
            NativeStrokeAdapter.Project(native, Matrix3x2.Identity, model, _viewport);
            _presented.Add(native.Id, new PresentedStroke(model, native, Matrix3x2.Identity));
            added.Add(native);
        }
        if (added.Count != 0)
            _presenter.StrokeContainer.AddStrokes(added);
    }

    private void OnStrokeStarted(Microsoft.UI.Xaml.Controls.InkStrokeInput sender, PointerEventArgs args)
    {
        if (_disposed) return;
        if (!IsCollecting)
        {
            _captureViewport = _renderedViewport;
            _discardCurrentStroke = false;
        }
        _inkingPointers.Add(args.CurrentPoint.PointerId);
        _awaitingCollectedStroke = true;
        _haptics.Start(args.CurrentPoint);
    }

    private void OnStrokeEnded(Microsoft.UI.Xaml.Controls.InkStrokeInput sender, PointerEventArgs args)
    {
        _inkingPointers.Remove(args.CurrentPoint.PointerId);
        _haptics.Stop();
        // Keep the viewport fixed until StrokesCollected delivers the completed native stroke.
        RequestViewUpdate();
    }

    private void OnStrokeCanceled(Microsoft.UI.Xaml.Controls.InkStrokeInput sender, PointerEventArgs args)
    {
        _inkingPointers.Remove(args.CurrentPoint.PointerId);
        _awaitingCollectedStroke = false;
        _discardCurrentStroke = false;
        _haptics.Stop();
        RequestViewUpdate();
    }

    private void OnStrokesCollected(NativeInkPresenter sender, InkStrokesCollectedEventArgs args)
    {
        if (_disposed) return;
        foreach (var native in args.Strokes)
        {
            if (_ignoredStrokes.Remove(native.Id) || _discardCurrentStroke)
            {
                native.Selected = true;
                _presenter.StrokeContainer.DeleteSelected();
                continue;
            }
            if (_presented.ContainsKey(native.Id)) continue;

            var captureViewport = IsCollecting ? _captureViewport : _renderedViewport;
            var model = NativeStrokeAdapter.Capture(native, captureViewport, DateTime.UtcNow.Ticks);
            var sourceToWorld = native.PointTransform * captureViewport.GetInverseTransformMatrix();
            _presented.Add(native.Id, new PresentedStroke(model, native, sourceToWorld));
            _strokes.Add(model);
            StrokeCompleted?.Invoke(this, new StrokeCompletedEventArgs { Stroke = model });
        }
        _awaitingCollectedStroke = false;
        _discardCurrentStroke = false;
        RequestViewUpdate();
    }

    private void OnStrokesErased(NativeInkPresenter sender, InkStrokesErasedEventArgs args)
    {
        if (_disposed) return;
        foreach (var native in args.Strokes)
        {
            if (_presented.Remove(native.Id, out var entry))
                _strokes.Remove(entry.Model);
        }
    }

    private void OnStrokesCollectionChanged(object? sender, EventArgs e)
    {
        RequestViewUpdate();
        StrokesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateDrawingAttributes()
    {
        if (!_initialized || _disposed) return;
        _presenter.UpdateDefaultDrawingAttributes(new InkDrawingAttributes
        {
            Color = PenColor,
            Size = new Size(PenThickness * _renderedViewport.Zoom, PenThickness * _renderedViewport.Zoom),
            IgnorePressure = false,
            IgnoreTilt = false
        });
    }

    private void UpdateInputDevices()
    {
        _presenter.InputDeviceTypes = CoreInputDeviceTypes.Pen |
            (IsMouseInkingEnabled || ToolMode == ToolMode.Pan ? CoreInputDeviceTypes.Mouse : CoreInputDeviceTypes.None);
    }

    private void UpdateToolbar()
    {
        if (!_initialized || _disposed || _updatingToolbar) return;
        _updatingToolbar = true;
        try
        {
            Toolbar.ActiveTool = ToolMode switch
            {
                ToolMode.Eraser => EraserButton,
                ToolMode.Pan => PanButton,
                _ => PenButton
            };
            PenButton.SelectedStrokeWidth = PenThickness;
            var index = PenButton.Palette
                .Select((brush, i) => (brush, i))
                .FirstOrDefault(p => p.brush is SolidColorBrush solid && solid.Color == PenColor, (null, -1)).i;
            if (index < 0)
            {
                PenButton.Palette.Add(new SolidColorBrush(PenColor));
                index = PenButton.Palette.Count - 1;
            }
            PenButton.SelectedBrushIndex = index;
            _presenter.InputProcessingConfiguration.Mode = ToolMode switch
            {
                ToolMode.Eraser => NativeProcessingMode.Erasing,
                ToolMode.Pan => NativeProcessingMode.None,
                _ => NativeProcessingMode.Inking
            };
            UpdateInputDevices();
            UpdateDrawingAttributes();
        }
        finally
        {
            _updatingToolbar = false;
        }
    }

    private void OnToolbarToolChanged(InkToolbar sender, object args)
    {
        if (_updatingToolbar || !_initialized) return;
        ToolMode = sender.ActiveTool == PanButton ? ToolMode.Pan :
            sender.ActiveTool == EraserButton ? ToolMode.Eraser : ToolMode.Pen;
    }

    private void OnToolbarAttributesChanged(InkToolbar sender, object args)
    {
        if (_updatingToolbar || !_initialized) return;
        _updatingToolbar = true;
        try
        {
            PenColor = sender.InkDrawingAttributes.Color;
            PenThickness = sender.InkDrawingAttributes.Size.Width;
        }
        finally
        {
            _updatingToolbar = false;
        }
        UpdateDrawingAttributes();
    }

    private void OnToolbarEraseAll(InkToolbar sender, object args) => Clear();

    private void ZoomIn_Click(object sender, RoutedEventArgs args) => SetZoom(_viewport.Zoom * ViewportState.ZoomInFactor);
    private void ZoomOut_Click(object sender, RoutedEventArgs args) => SetZoom(_viewport.Zoom * ViewportState.ZoomOutFactor);

    private void OnUnprocessedPressed(Microsoft.UI.Xaml.Controls.InkUnprocessedInput sender, PointerEventArgs args)
    {
        if (_disposed || IsCollecting) return;
        var point = args.CurrentPoint;
        if (ToolMode != ToolMode.Pan && !point.Properties.IsRightButtonPressed &&
            !point.Properties.IsMiddleButtonPressed && !point.Properties.IsBarrelButtonPressed)
            return;
        _panPointer = point.PointerId;
        _panPosition = new Vector2((float)point.Position.X, (float)point.Position.Y);
    }

    private void OnUnprocessedMoved(Microsoft.UI.Xaml.Controls.InkUnprocessedInput sender, PointerEventArgs args)
    {
        if (_panPointer != args.CurrentPoint.PointerId) return;
        var point = args.CurrentPoint.Position;
        var position = new Vector2((float)point.X, (float)point.Y);
        SetViewport(_viewport.AddPan(position - _panPosition));
        _panPosition = position;
    }

    private void OnUnprocessedReleased(Microsoft.UI.Xaml.Controls.InkUnprocessedInput sender, PointerEventArgs args)
    {
        if (_panPointer == args.CurrentPoint.PointerId)
            _panPointer = null;
    }

    private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        e.Handled = true;
        if (IsCollecting) return;
        var point = e.GetCurrentPoint(ViewportGrid);
        var delta = point.Properties.MouseWheelDelta;
        if (e.KeyModifiers.HasFlag(Windows.System.VirtualKeyModifiers.Control))
        {
            var center = new Vector2((float)point.Position.X, (float)point.Position.Y);
            ZoomAt(_viewport.Zoom * MathF.Pow(1.1f, delta / 120f), center);
        }
        else
        {
            var horizontal = point.Properties.IsHorizontalMouseWheel ||
                e.KeyModifiers.HasFlag(Windows.System.VirtualKeyModifiers.Shift);
            PanBy(horizontal ? delta / 2f : 0, horizontal ? 0 : delta / 2f);
        }
    }

    private void OnTouchPressed(object sender, PointerRoutedEventArgs e)
    {
        if (e.Pointer.PointerDeviceType != PointerDeviceType.Touch || IsCollecting) return;
        var point = e.GetCurrentPoint(ViewportGrid).Position;
        if (!ViewportGrid.CapturePointer(e.Pointer)) return;
        _touches[e.Pointer.PointerId] = new Vector2((float)point.X, (float)point.Y);
        _presenter.IsInputEnabled = false;
        e.Handled = true;
    }

    private void OnTouchMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_touches.ContainsKey(e.Pointer.PointerId)) return;
        var oldPoints = _touches.Values.ToArray();
        var point = e.GetCurrentPoint(ViewportGrid).Position;
        _touches[e.Pointer.PointerId] = new Vector2((float)point.X, (float)point.Y);
        var newPoints = _touches.Values.ToArray();
        var oldCenter = oldPoints.Aggregate(Vector2.Zero, (sum, p) => sum + p) / oldPoints.Length;
        var newCenter = newPoints.Aggregate(Vector2.Zero, (sum, p) => sum + p) / newPoints.Length;
        var view = _viewport;
        if (oldPoints.Length >= 2)
        {
            var oldDistance = Vector2.Distance(oldPoints[0], oldPoints[1]);
            var newDistance = Vector2.Distance(newPoints[0], newPoints[1]);
            if (oldDistance > 5 && newDistance > 5)
                view = view.ZoomAroundPoint(view.Zoom * newDistance / oldDistance, oldCenter);
        }
        SetViewport(view.AddPan(newCenter - oldCenter));
        e.Handled = true;
    }

    private void OnTouchReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_touches.Remove(e.Pointer.PointerId)) return;
        ViewportGrid.ReleasePointerCapture(e.Pointer);
        if (_touches.Count == 0)
            _presenter.IsInputEnabled = !_disposed;
        e.Handled = true;
    }

    private void SetViewport(ViewportState viewport)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!float.IsFinite(viewport.Pan.X) || !float.IsFinite(viewport.Pan.Y) || !float.IsFinite(viewport.Zoom))
            throw new ArgumentOutOfRangeException(nameof(viewport), "Viewport coordinates must be finite.");
        _viewport = viewport.WithZoom(viewport.Zoom);
        _updatingProperties = true;
        try
        {
            PanX = _viewport.Pan.X;
            PanY = _viewport.Pan.Y;
            Zoom = _viewport.Zoom;
        }
        finally
        {
            _updatingProperties = false;
        }
        RequestViewUpdate();
    }

    private void DrawBackground()
    {
        var geometry = new GeometryGroup();
        var color = Color.FromArgb((byte)(BgColorArgb >> 24), (byte)(BgColorArgb >> 16),
            (byte)(BgColorArgb >> 8), (byte)BgColorArgb);
        var brush = new SolidColorBrush(color);
        BackgroundPattern.Stroke = BgType == BackgroundType.Ruled ? brush : null;
        BackgroundPattern.Fill = BgType == BackgroundType.Dotted ? brush : null;
        BackgroundPattern.StrokeThickness = 1;

        if (BgType != BackgroundType.Blank)
        {
            var step = BgSpacing * _renderedViewport.Zoom;
            // Thin the world-anchored lattice at low zoom instead of creating an unbounded
            // number of XAML geometries. Dot size and ruled-line weight stay screen-constant.
            while (step < 12) step *= 2;
            var startX = ((_renderedViewport.Pan.X % step) + step) % step;
            var startY = ((_renderedViewport.Pan.Y % step) + step) % step;
            for (var y = startY; y < ViewportGrid.ActualHeight; y += step)
            {
                if (BgType == BackgroundType.Ruled)
                    geometry.Children.Add(new LineGeometry
                    {
                        StartPoint = new Point(0, y), EndPoint = new Point(ViewportGrid.ActualWidth, y)
                    });
                else
                    for (var x = startX; x < ViewportGrid.ActualWidth; x += step)
                        geometry.Children.Add(new EllipseGeometry
                        {
                            Center = new Point(x, y), RadiusX = 1.25, RadiusY = 1.25
                        });
            }
        }
        BackgroundPattern.Data = geometry;
    }

    private static void OnPenPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var canvas = (InkCanvas)d;
        if (!double.IsFinite(canvas.PenThickness) || canvas.PenThickness <= 0 || canvas.PenThickness > 100)
            throw new ArgumentOutOfRangeException(nameof(PenThickness), "Pen width must be in (0, 100] world DIPs.");
        canvas.UpdateToolbar();
        canvas.UpdateDrawingAttributes();
    }

    private static void OnToolModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((InkCanvas)d).UpdateToolbar();

    private static void OnInputDevicesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var canvas = (InkCanvas)d;
        if (canvas._initialized) canvas.UpdateInputDevices();
    }

    private static void OnBackgroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var canvas = (InkCanvas)d;
        if (!double.IsFinite(canvas.BgSpacing) || canvas.BgSpacing < 1)
            throw new ArgumentOutOfRangeException(nameof(BgSpacing), "Background spacing must be at least one world DIP.");
        canvas.RequestViewUpdate();
    }

    private static void OnViewportChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var canvas = (InkCanvas)d;
        if (canvas._updatingProperties) return;
        canvas.SetViewport(new ViewportState
        {
            Pan = new Vector2((float)canvas.PanX, (float)canvas.PanY), Zoom = (float)canvas.Zoom
        });
    }

    public bool HasStrokes => _strokes.Strokes.Count != 0;
    public StrokeCollection GetStrokes() => _strokes;

    public void SetStrokes(StrokeCollection strokes)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(strokes);
        DiscardPendingInk();
        _strokes.StrokesChanged -= OnStrokesCollectionChanged;
        _strokes = strokes;
        _strokes.StrokesChanged += OnStrokesCollectionChanged;
        RequestViewUpdate();
        StrokesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void DiscardPendingInk()
    {
        foreach (var native in _presenter.StrokeContainer.GetStrokes())
            if (!_presented.ContainsKey(native.Id)) _ignoredStrokes.Add(native.Id);
        _discardCurrentStroke = IsCollecting;
    }

    public void Clear()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        DiscardPendingInk();
        _presented.Clear();
        _presenter.StrokeContainer.Clear();
        _strokes.Clear();
        RequestViewUpdate();
    }

    public void PanBy(float x, float y) => SetViewport(_viewport.AddPan(new Vector2(x, y)));

    public void ZoomAt(float zoom, Vector2 center)
    {
        if (!float.IsFinite(zoom) || !float.IsFinite(center.X) || !float.IsFinite(center.Y))
            throw new ArgumentOutOfRangeException(nameof(zoom), "Zoom and focal point must be finite.");
        SetViewport(_viewport.ZoomAroundPoint(zoom, center));
    }

    public void SetZoom(float zoom) =>
        ZoomAt(zoom, new Vector2((float)ViewportGrid.ActualWidth / 2, (float)ViewportGrid.ActualHeight / 2));

    public void ResetViewport() => SetViewport(ViewportState.Default);

    public Task<byte[]> ExportAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return Task.FromResult(StrokeSerializer.Serialize(_strokes));
    }

    public Task ImportAsync(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        SetStrokes(StrokeSerializer.Deserialize(data));
        return Task.CompletedTask;
    }

    public Task<byte[]> SaveInkAsync() => ExportAsync();
    public Task LoadInkAsync(byte[] inkData) => ImportAsync(inkData);
    public void ClearStrokes() => Clear();

    public void AddRandomStrokes(int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        for (var i = 0; i < count; i++)
        {
            var stroke = new Stroke(PenColor.ToStrokeColor(), (float)PenThickness);
            var screen = new Vector2(
                Random.Shared.NextSingle() * (float)ViewportGrid.ActualWidth,
                Random.Shared.NextSingle() * (float)ViewportGrid.ActualHeight);
            for (var p = 0; p < 20; p++)
            {
                screen += new Vector2(Random.Shared.NextSingle() * 30 - 15, Random.Shared.NextSingle() * 30 - 15);
                stroke.AddPoint(new StrokePoint(_viewport.ScreenToWorld(screen), 0.5f, DateTime.UtcNow.Ticks));
            }
            _strokes.Add(stroke);
            StrokeCompleted?.Invoke(this, new StrokeCompletedEventArgs { Stroke = stroke });
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        CancelQueuedFrame();
        _haptics.Stop();
        _presenter.IsInputEnabled = false;
        _presenter.StrokesCollected -= OnStrokesCollected;
        _presenter.StrokesErased -= OnStrokesErased;
        _presenter.StrokeInput.StrokeStarted -= OnStrokeStarted;
        _presenter.StrokeInput.StrokeEnded -= OnStrokeEnded;
        _presenter.StrokeInput.StrokeCanceled -= OnStrokeCanceled;
        _presenter.UnprocessedInput.PointerPressed -= OnUnprocessedPressed;
        _presenter.UnprocessedInput.PointerMoved -= OnUnprocessedMoved;
        _presenter.UnprocessedInput.PointerReleased -= OnUnprocessedReleased;
        _presenter.UnprocessedInput.PointerLost -= OnUnprocessedReleased;
        Toolbar.ActiveToolChanged -= OnToolbarToolChanged;
        Toolbar.InkDrawingAttributesChanged -= OnToolbarAttributesChanged;
        Toolbar.EraseAllClicked -= OnToolbarEraseAll;
        _strokes.StrokesChanged -= OnStrokesCollectionChanged;
        ViewportGrid.ReleasePointerCaptures();
        _touches.Clear();
        _presented.Clear();
    }
}
