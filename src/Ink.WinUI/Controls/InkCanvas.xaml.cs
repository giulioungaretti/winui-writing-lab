using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using InkControl.Input;
using InkControl.Models;
using InkControl.Rendering;
using inkapp.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Vortice.Mathematics;
using WinRT;
using StrokeSerializer = InkControl.Models.StrokeSerializer;

namespace InkControl.Controls;

/// <summary>
/// High-performance ink drawing surface using Direct2D.
/// </summary>
/// <remarks>
/// This control provides a complete ink drawing experience with:
/// <list type="bullet">
///   <item>Pen/touch drawing with pressure sensitivity</item>
///   <item>Stroke erasing</item>
///   <item>Pan and zoom navigation</item>
///   <item>Configurable background patterns</item>
/// </list>
/// </remarks>
public sealed partial class InkCanvas : UserControl, IDisposable
{
    private SwapChainPanel? _swapChainPanel;
    private SwapChainManager? _swapChainManager;
    private D2DInkRenderer? _renderer;
    private InkInputHandler? _inputHandler;
    private StrokeCollection? _strokes;
    private ViewportState _viewport = ViewportState.Default;
    private bool _isInitialized;
    private bool _isDisposed;
    private bool _needsRedraw;
    private bool _isRendering;
    private Grid? _rootGrid;
    private bool _isUpdatingViewport;

    private readonly ILogger<InkCanvas>? _logger;

    #region Dependency Properties

    /// <summary>
    /// Identifies the <see cref="PenColor"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty PenColorProperty =
        DependencyProperty.Register(nameof(PenColor), typeof(Windows.UI.Color), typeof(InkCanvas),
            new PropertyMetadata(Microsoft.UI.Colors.Black, OnPenPropertyChanged));

    /// <summary>
    /// Gets or sets the pen color for drawing.
    /// </summary>
    public Windows.UI.Color PenColor
    {
        get => (Windows.UI.Color)GetValue(PenColorProperty);
        set => SetValue(PenColorProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="PenThickness"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty PenThicknessProperty =
        DependencyProperty.Register(nameof(PenThickness), typeof(double), typeof(InkCanvas),
            new PropertyMetadata(2.0, OnPenPropertyChanged));

    /// <summary>
    /// Gets or sets the pen stroke thickness in pixels.
    /// </summary>
    public double PenThickness
    {
        get => (double)GetValue(PenThicknessProperty);
        set => SetValue(PenThicknessProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="ToolMode"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ToolModeProperty =
        DependencyProperty.Register(nameof(ToolMode), typeof(ToolMode), typeof(InkCanvas),
            new PropertyMetadata(ToolMode.Pen, OnToolModeChanged));

    /// <summary>
    /// Gets or sets the current tool mode (Pen, Eraser, or Pan).
    /// </summary>
    public ToolMode ToolMode
    {
        get => (ToolMode)GetValue(ToolModeProperty);
        set => SetValue(ToolModeProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="EraserRadius"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty EraserRadiusProperty =
        DependencyProperty.Register(nameof(EraserRadius), typeof(double), typeof(InkCanvas),
            new PropertyMetadata(10.0));

    /// <summary>
    /// Gets or sets the eraser hit-test radius in pixels.
    /// </summary>
    public double EraserRadius
    {
        get => (double)GetValue(EraserRadiusProperty);
        set => SetValue(EraserRadiusProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="BgType"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty BgTypeProperty =
        DependencyProperty.Register(nameof(BgType), typeof(BackgroundType), typeof(InkCanvas),
            new PropertyMetadata(BackgroundType.Blank, OnBackgroundChanged));

    /// <summary>
    /// Gets or sets the background pattern type.
    /// </summary>
    public BackgroundType BgType
    {
        get => (BackgroundType)GetValue(BgTypeProperty);
        set => SetValue(BgTypeProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="BgSpacing"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty BgSpacingProperty =
        DependencyProperty.Register(nameof(BgSpacing), typeof(double), typeof(InkCanvas),
            new PropertyMetadata(24.0, OnBackgroundChanged));

    /// <summary>
    /// Gets or sets the background pattern spacing in pixels.
    /// </summary>
    public double BgSpacing
    {
        get => (double)GetValue(BgSpacingProperty);
        set => SetValue(BgSpacingProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="BgColorArgb"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty BgColorArgbProperty =
        DependencyProperty.Register(nameof(BgColorArgb), typeof(int), typeof(InkCanvas),
            new PropertyMetadata(D2DBackgroundSettings.DefaultPatternColorArgb, OnBackgroundChanged));

    /// <summary>
    /// Gets or sets the background pattern color as packed ARGB integer.
    /// </summary>
    public int BgColorArgb
    {
        get => (int)GetValue(BgColorArgbProperty);
        set => SetValue(BgColorArgbProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="PanX"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty PanXProperty =
        DependencyProperty.Register(nameof(PanX), typeof(double), typeof(InkCanvas),
            new PropertyMetadata(0.0, OnViewportChanged));

    /// <summary>
    /// Gets or sets the horizontal pan offset.
    /// </summary>
    public double PanX
    {
        get => (double)GetValue(PanXProperty);
        set => SetValue(PanXProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="PanY"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty PanYProperty =
        DependencyProperty.Register(nameof(PanY), typeof(double), typeof(InkCanvas),
            new PropertyMetadata(0.0, OnViewportChanged));

    /// <summary>
    /// Gets or sets the vertical pan offset.
    /// </summary>
    public double PanY
    {
        get => (double)GetValue(PanYProperty);
        set => SetValue(PanYProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="Zoom"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ZoomProperty =
        DependencyProperty.Register(nameof(Zoom), typeof(double), typeof(InkCanvas),
            new PropertyMetadata(1.0, OnViewportChanged));

    /// <summary>
    /// Gets or sets the zoom level (1.0 = 100%).
    /// </summary>
    public double Zoom
    {
        get => (double)GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    #endregion

    #region Events

    /// <summary>
    /// Occurs when strokes are added or removed.
    /// </summary>
    public event EventHandler? StrokesChanged;

    /// <summary>
    /// Occurs when a stroke is completed.
    /// </summary>
    public event EventHandler<StrokeCompletedEventArgs>? StrokeCompleted;

    /// <summary>
    /// Occurs when the viewport (pan/zoom) changes.
    /// </summary>
    public event EventHandler<ViewportChangedEventArgs>? ViewportChanged;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="InkCanvas"/> class.
    /// </summary>
    public InkCanvas() : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InkCanvas"/> class with optional logging.
    /// </summary>
    /// <param name="loggerFactory">Optional logger factory for diagnostic logging.</param>
    public InkCanvas(ILoggerFactory? loggerFactory)
    {
        _logger = loggerFactory?.CreateLogger<InkCanvas>();

        InitializeComponent();

        _rootGrid = RootGrid;
        _strokes = new StrokeCollection();
        _strokes.StrokesChanged += OnStrokesCollectionChanged;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += OnSizeChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _logger?.LogDebug("InkCanvas loaded");
        InitializeRendering();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Dispose();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!_isInitialized || _swapChainManager is null) return;

        var width = (uint)Math.Max(1, ActualWidth);
        var height = (uint)Math.Max(1, ActualHeight);

        try
        {
            _swapChainManager.Resize(width, height);
            RequestRedraw();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to resize swap chain");
        }
    }

    #region Initialization

    private void InitializeRendering()
    {
        if (_isInitialized || _rootGrid is null) return;

        try
        {
            var deviceManager = DeviceManager.Instance;
            deviceManager.DeviceLost += OnDeviceLost;
            deviceManager.DeviceRestored += OnDeviceRestored;

            // Create swap chain
            var width = (uint)Math.Max(1, ActualWidth);
            var height = (uint)Math.Max(1, ActualHeight);
            _swapChainManager = new SwapChainManager(deviceManager, width, height);

            // Create swap chain panel and set the swap chain
            _swapChainPanel = new SwapChainPanel();
            SetSwapChainOnPanel(_swapChainPanel, _swapChainManager.SwapChain);
            _rootGrid.Children.Add(_swapChainPanel);

            // Create renderer
            _renderer = new D2DInkRenderer(_swapChainManager, deviceManager);

            // Create input handler
            _inputHandler = new InkInputHandler
            {
                Viewport = _viewport,
                PenColor = PenColor,
                PenThickness = (float)PenThickness,
                Mode = ToolMode,
                EraserTolerance = (float)EraserRadius
            };

            // Wire up pointer events for pen input and mouse wheel
            _swapChainPanel.PointerPressed += _inputHandler.OnPointerPressed;
            _swapChainPanel.PointerMoved += _inputHandler.OnPointerMoved;
            _swapChainPanel.PointerReleased += _inputHandler.OnPointerReleased;
            _swapChainPanel.PointerCanceled += _inputHandler.OnPointerCanceled;
            _swapChainPanel.PointerWheelChanged += _inputHandler.OnPointerWheelChanged;
            _swapChainPanel.PointerEntered += _inputHandler.OnPointerEntered;
            _swapChainPanel.PointerExited += _inputHandler.OnPointerExited;

            // Enable XAML manipulation events for touch and touchpad pan/zoom with inertia
            _swapChainPanel.ManipulationMode =
                ManipulationModes.TranslateX |
                ManipulationModes.TranslateY |
                ManipulationModes.Scale |
                ManipulationModes.TranslateInertia |
                ManipulationModes.ScaleInertia;

            _swapChainPanel.ManipulationStarted += _inputHandler.OnManipulationStarted;
            _swapChainPanel.ManipulationDelta += _inputHandler.OnManipulationDelta;
            _swapChainPanel.ManipulationInertiaStarting += _inputHandler.OnManipulationInertiaStarting;
            _swapChainPanel.ManipulationCompleted += _inputHandler.OnManipulationCompleted;

            _inputHandler.StrokeCompleted += OnStrokeCompletedHandler;
            _inputHandler.PanDelta += OnPanDelta;
            _inputHandler.ZoomDelta += OnZoomDelta;
            _inputHandler.RedrawRequested += OnRedrawRequested;
            _inputHandler.StrokeEraseRequested += OnStrokeEraseRequested;

            _isInitialized = true;

            // Initial render
            RequestRedraw();

            _logger?.LogDebug("D2D rendering initialized");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize D2D rendering");
            throw;
        }
    }

    private static void SetSwapChainOnPanel(SwapChainPanel panel, nint swapChain)
    {
        // Get the ISwapChainPanelNative interface using WinRT interop
        var panelNative = panel.As<ISwapChainPanelNative>();
        panelNative.SetSwapChain(swapChain);
    }

    [ComImport]
    [Guid("63aad0b8-7c24-40ff-85a8-640d944cc325")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ISwapChainPanelNative
    {
        void SetSwapChain(nint swapChain);
    }

    #endregion

    #region Rendering

    private void RequestRedraw()
    {
        if (!_isInitialized) return;

        _needsRedraw = true;

        // During active drawing, render immediately for lowest latency.
        // Otherwise, queue to dispatcher for next frame.
        if (_inputHandler?.IsDrawing == true)
        {
            RenderImmediate();
        }
        else
        {
            DispatcherQueue.TryEnqueue(Render);
        }
    }

    /// <summary>
    /// Renders immediately without going through the dispatcher queue.
    /// Used during active drawing for lowest latency.
    /// </summary>
    private void RenderImmediate()
    {
        if (!_isInitialized || _isRendering) return;
        if (_renderer is null || _swapChainManager is null || _strokes is null) return;

        _isRendering = true;
        _needsRedraw = false;

        try
        {
            var bgSettings = new D2DBackgroundSettings(
                BgType,
                (float)BgSpacing,
                GetPatternColor(),
                new Color4(1f, 1f, 1f, 1f));

            _renderer.Render(_strokes, _inputHandler?.CurrentStroke, _viewport, bgSettings, isActivelyDrawing: true);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Immediate render failed");
        }
        finally
        {
            _isRendering = false;
        }
    }

    private void Render()
    {
        if (!_isInitialized || !_needsRedraw || _isRendering) return;
        if (_renderer is null || _swapChainManager is null || _strokes is null) return;

        _isRendering = true;
        _needsRedraw = false;

        try
        {
            // Create background settings from dependency properties
            var bgSettings = new D2DBackgroundSettings(
                BgType,
                (float)BgSpacing,
                GetPatternColor(),
                new Color4(1f, 1f, 1f, 1f)); // White background

            // Check if actively drawing - disable vsync for lower latency during inking
            bool isActivelyDrawing = _inputHandler?.IsDrawing == true;

            // Render with background
            _renderer.Render(_strokes, _inputHandler?.CurrentStroke, _viewport, bgSettings, isActivelyDrawing);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Render failed");
        }
        finally
        {
            _isRendering = false;
        }
    }

    /// <summary>
    /// Converts the <see cref="BgColorArgb"/> dependency property value to a <see cref="Color4"/> structure
    /// for use with Direct2D rendering.
    /// </summary>
    private Color4 GetPatternColor()
    {
        var argb = BgColorArgb;
        float a = ((argb >> 24) & 0xFF) / 255f;
        float r = ((argb >> 16) & 0xFF) / 255f;
        float g = ((argb >> 8) & 0xFF) / 255f;
        float b = (argb & 0xFF) / 255f;
        return new Color4(r, g, b, a);
    }

    #endregion

    #region Input Handling

    private void OnStrokeCompletedHandler(object? sender, Stroke stroke)
    {
        _strokes?.Add(stroke);
        StrokeCompleted?.Invoke(this, new StrokeCompletedEventArgs { Stroke = stroke });
        RequestRedraw();
    }

    private void OnStrokeEraseRequested(object? sender, (Vector2 Position, float Tolerance) args)
    {
        if (_strokes is null) return;

        var strokesToRemove = _strokes.GetStrokesAt(args.Position, args.Tolerance).ToList();

        if (strokesToRemove.Count > 0)
        {
            foreach (var stroke in strokesToRemove)
            {
                _strokes.Remove(stroke);
            }
            RequestRedraw();
        }
    }

    private void OnPanDelta(object? sender, Vector2 delta)
    {
        if (delta == Vector2.Zero) return;

        _logger?.LogTrace("PanDelta: Delta=({DeltaX:F2}, {DeltaY:F2}), Before=({BeforeX:F2}, {BeforeY:F2})",
            delta.X, delta.Y, _viewport.Pan.X, _viewport.Pan.Y);

        _viewport = _viewport.AddPan(delta);

        _logger?.LogTrace("PanDelta: After=({AfterX:F2}, {AfterY:F2})", _viewport.Pan.X, _viewport.Pan.Y);

        // Prevent re-entrancy from two-way binding callbacks
        _isUpdatingViewport = true;
        try
        {
            PanX = _viewport.Pan.X;
            PanY = _viewport.Pan.Y;
        }
        finally
        {
            _isUpdatingViewport = false;
        }

        if (_inputHandler is not null)
        {
            _inputHandler.Viewport = _viewport;
        }

        _renderer?.InvalidateStaticContent();

        RequestRedraw();
        ViewportChanged?.Invoke(this, new ViewportChangedEventArgs
        {
            PanX = (float)PanX,
            PanY = (float)PanY,
            Zoom = (float)Zoom
        });
    }

    private void OnZoomDelta(object? sender, (float Scale, Vector2 Center) args)
    {
        float newZoom = _viewport.Zoom * args.Scale;
        newZoom = Math.Clamp(newZoom, ViewportState.MinZoom, ViewportState.MaxZoom);

        if (Math.Abs(newZoom - _viewport.Zoom) < 0.001f)
            return;

        _logger?.LogTrace("ZoomDelta: Scale={Scale:F3}, Center=({CenterX:F1}, {CenterY:F1}), NewZoom={NewZoom:F3}",
            args.Scale, args.Center.X, args.Center.Y, newZoom);

        _viewport = _viewport.ZoomAroundPoint(newZoom, args.Center);

        _isUpdatingViewport = true;
        try
        {
            PanX = _viewport.Pan.X;
            PanY = _viewport.Pan.Y;
            Zoom = _viewport.Zoom;
        }
        finally
        {
            _isUpdatingViewport = false;
        }

        if (_inputHandler is not null)
        {
            _inputHandler.Viewport = _viewport;
        }

        _renderer?.InvalidateStaticContent();

        RequestRedraw();
        ViewportChanged?.Invoke(this, new ViewportChangedEventArgs
        {
            PanX = (float)PanX,
            PanY = (float)PanY,
            Zoom = (float)Zoom
        });
    }

    private void OnRedrawRequested(object? sender, EventArgs e)
    {
        RequestRedraw();
    }

    #endregion

    #region Property Change Handlers

    private static void OnPenPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (InkCanvas)d;
        if (control._inputHandler is not null)
        {
            control._inputHandler.PenColor = control.PenColor;
            control._inputHandler.PenThickness = (float)control.PenThickness;
        }
    }

    private static void OnToolModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (InkCanvas)d;
        if (control._inputHandler is not null)
        {
            control._inputHandler.Mode = (ToolMode)e.NewValue;
        }
    }

    private static void OnBackgroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (InkCanvas)d;
        control.RequestRedraw();
    }

    private static void OnViewportChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (InkCanvas)d;

        if (control._isUpdatingViewport)
            return;

        var newPan = new Vector2((float)control.PanX, (float)control.PanY);
        var newZoom = (float)control.Zoom;

        bool panChanged = Vector2.Distance(newPan, control._viewport.Pan) > 0.01f;
        bool zoomChanged = Math.Abs(newZoom - control._viewport.Zoom) > 0.001f;

        if (panChanged || zoomChanged)
        {
            control._viewport = new ViewportState
            {
                Pan = newPan,
                Zoom = Math.Clamp(newZoom, ViewportState.MinZoom, ViewportState.MaxZoom)
            };

            if (control._inputHandler is not null)
            {
                control._inputHandler.Viewport = control._viewport;
            }

            // Invalidate static content when viewport changes
            control._renderer?.InvalidateStaticContent();

            control.RequestRedraw();
        }
    }

    #endregion

    #region Device Lost Handling

    private void OnDeviceLost(object? sender, EventArgs e)
    {
        _logger?.LogWarning("D3D device lost, will recreate");
        _renderer?.ClearBrushCache();
    }

    private void OnDeviceRestored(object? sender, EventArgs e)
    {
        _logger?.LogInformation("D3D device restored");

        if (_swapChainManager is not null)
        {
            _swapChainManager.Dispose();

            var width = (uint)Math.Max(1, ActualWidth);
            var height = (uint)Math.Max(1, ActualHeight);
            _swapChainManager = new SwapChainManager(DeviceManager.Instance, width, height);

            if (_swapChainPanel is not null)
            {
                SetSwapChainOnPanel(_swapChainPanel, _swapChainManager.SwapChain);
            }

            _renderer = new D2DInkRenderer(_swapChainManager, DeviceManager.Instance);
        }

        RequestRedraw();
    }

    #endregion

    #region Stroke Collection Changed

    private void OnStrokesCollectionChanged(object? sender, EventArgs e)
    {
        StrokesChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets whether the canvas has any strokes.
    /// </summary>
    public bool HasStrokes => _strokes is not null && _strokes.Strokes.Count > 0;

    /// <summary>
    /// Gets the current stroke collection.
    /// </summary>
    /// <returns>The stroke collection.</returns>
    public StrokeCollection GetStrokes() => _strokes ?? new StrokeCollection();

    /// <summary>
    /// Sets the stroke collection.
    /// </summary>
    /// <param name="strokes">The strokes to set.</param>
    public void SetStrokes(StrokeCollection strokes)
    {
        ArgumentNullException.ThrowIfNull(strokes);

        if (_strokes is not null)
        {
            _strokes.StrokesChanged -= OnStrokesCollectionChanged;
        }

        _strokes = strokes;
        _strokes.StrokesChanged += OnStrokesCollectionChanged;

        RequestRedraw();
    }

    /// <summary>
    /// Clears all strokes from the canvas.
    /// </summary>
    public void Clear()
    {
        _strokes?.Clear();
        RequestRedraw();
    }

    /// <summary>
    /// Sets the zoom level programmatically.
    /// </summary>
    /// <param name="zoom">The zoom level (1.0 = 100%).</param>
    public void SetZoom(float zoom)
    {
        Zoom = Math.Clamp(zoom, ViewportState.MinZoom, ViewportState.MaxZoom);
    }

    /// <summary>
    /// Resets the viewport to default state (no pan, 100% zoom).
    /// </summary>
    public void ResetViewport()
    {
        _viewport = ViewportState.Default;
        _isUpdatingViewport = true;
        try
        {
            PanX = 0;
            PanY = 0;
            Zoom = 1.0;
        }
        finally
        {
            _isUpdatingViewport = false;
        }

        if (_inputHandler is not null)
        {
            _inputHandler.Viewport = _viewport;
        }

        _renderer?.InvalidateStaticContent();
        RequestRedraw();
    }

    /// <summary>
    /// Exports strokes to byte array for persistence.
    /// </summary>
    /// <returns>Serialized stroke data.</returns>
    public Task<byte[]> ExportAsync()
    {
        if (_strokes is null || _strokes.Strokes.Count == 0)
        {
            _logger?.LogDebug("ExportAsync: No strokes to save");
            return Task.FromResult(Array.Empty<byte>());
        }

        var data = StrokeSerializer.Serialize(_strokes);
        _logger?.LogDebug("ExportAsync: Serialized {StrokeCount} strokes to {ByteCount} bytes",
            _strokes.Strokes.Count, data.Length);
        return Task.FromResult(data);
    }

    /// <summary>
    /// Imports strokes from byte array.
    /// </summary>
    /// <param name="data">Serialized stroke data.</param>
    public Task ImportAsync(byte[] data)
    {
        if (data is null || data.Length == 0)
        {
            _logger?.LogDebug("ImportAsync: No ink data provided, clearing canvas");
            Clear();
            return Task.CompletedTask;
        }

        var loadedStrokes = StrokeSerializer.Deserialize(data);
        _logger?.LogDebug("ImportAsync: Deserialized {StrokeCount} strokes from {ByteCount} bytes",
            loadedStrokes.Strokes.Count, data.Length);

        if (_strokes is not null)
        {
            _strokes.StrokesChanged -= OnStrokesCollectionChanged;
        }

        _strokes = loadedStrokes;
        _strokes.StrokesChanged += OnStrokesCollectionChanged;

        RequestRedraw();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Saves ink data asynchronously (alias for <see cref="ExportAsync"/>).
    /// </summary>
    /// <returns>Serialized stroke data.</returns>
    public Task<byte[]> SaveInkAsync() => ExportAsync();

    /// <summary>
    /// Loads ink data asynchronously (alias for <see cref="ImportAsync"/>).
    /// </summary>
    /// <param name="inkData">Serialized stroke data.</param>
    public Task LoadInkAsync(byte[] inkData) => ImportAsync(inkData);

    /// <summary>
    /// Clears all strokes (alias for <see cref="Clear"/>).
    /// </summary>
    public void ClearStrokes() => Clear();

    /// <summary>
    /// Adds random strokes for testing by simulating pen input.
    /// Goes through the full input pipeline including viewport transforms.
    /// </summary>
    /// <param name="count">Number of random strokes to add.</param>
    public void AddRandomStrokes(int count)
    {
        if (_inputHandler is null || !_isInitialized) return;

        var width = (float)Math.Max(100, ActualWidth);
        var height = (float)Math.Max(100, ActualHeight);

        _inputHandler.SimulateRandomStrokes(count, width, height);
    }

    #endregion

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (_inputHandler is not null && _swapChainPanel is not null)
        {
            _swapChainPanel.PointerPressed -= _inputHandler.OnPointerPressed;
            _swapChainPanel.PointerMoved -= _inputHandler.OnPointerMoved;
            _swapChainPanel.PointerReleased -= _inputHandler.OnPointerReleased;
            _swapChainPanel.PointerCanceled -= _inputHandler.OnPointerCanceled;
            _swapChainPanel.PointerWheelChanged -= _inputHandler.OnPointerWheelChanged;
            _swapChainPanel.PointerEntered -= _inputHandler.OnPointerEntered;
            _swapChainPanel.PointerExited -= _inputHandler.OnPointerExited;

            _swapChainPanel.ManipulationStarted -= _inputHandler.OnManipulationStarted;
            _swapChainPanel.ManipulationDelta -= _inputHandler.OnManipulationDelta;
            _swapChainPanel.ManipulationInertiaStarting -= _inputHandler.OnManipulationInertiaStarting;
            _swapChainPanel.ManipulationCompleted -= _inputHandler.OnManipulationCompleted;

            _inputHandler.StrokeCompleted -= OnStrokeCompletedHandler;
            _inputHandler.PanDelta -= OnPanDelta;
            _inputHandler.ZoomDelta -= OnZoomDelta;
            _inputHandler.RedrawRequested -= OnRedrawRequested;
            _inputHandler.StrokeEraseRequested -= OnStrokeEraseRequested;
        }

        if (_strokes is not null)
            _strokes.StrokesChanged -= OnStrokesCollectionChanged;

        var deviceManager = DeviceManager.Instance;
        deviceManager.DeviceLost -= OnDeviceLost;
        deviceManager.DeviceRestored -= OnDeviceRestored;

        _renderer?.Dispose();
        _swapChainManager?.Dispose();

        _rootGrid?.Children.Clear();
        _swapChainPanel = null;

        _isInitialized = false;
    }
}
