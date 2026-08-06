using System;
using System.Collections.Generic;
using System.Numerics;
using InkControl.Models;
using inkapp.Core.Input;
using inkapp.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.Devices.Haptics;
using PointerDeviceType = Microsoft.UI.Input.PointerDeviceType;

namespace InkControl.Input;

/// <summary>
/// Handles pointer input for ink drawing, erasing, and navigation.
/// Uses XAML ManipulationMode for smooth pan/zoom with inertia support.
/// Pen input is handled separately for drawing with haptic feedback and tilt support.
/// </summary>
internal sealed class InkInputHandler : IDisposable
{
    private readonly ILogger<InkInputHandler>? _logger;
    private readonly IInkInputProcessor _inkProcessor;
    private readonly ViewportStateAdapter _viewportAdapter;
    private UIElement? _element;
    private bool _disposed;

    // Track active touch contacts for determining multi-touch gestures
    private readonly HashSet<uint> _activeTouchPointers = [];


    // Haptic feedback support
    private SimpleHapticsController? _hapticsController;
    private SimpleHapticsControllerFeedback? _inkingWaveform;

    // Eraser state
    private bool _isErasing;
    private uint? _eraserPointerId;

    // Viewport state - the adapter reads from this
    private ViewportState? _viewport;

    /// <summary>
    /// Gets or sets the viewport state for coordinate transforms.
    /// </summary>
    public ViewportState? Viewport
    {
        get => _viewport;
        set => _viewport = value;
    }

    /// <summary>
    /// Gets or sets the current pen color.
    /// </summary>
    public Windows.UI.Color PenColor
    {
        get => _inkProcessor.PenColor.ToWindowsColor();
        set => _inkProcessor.PenColor = value.ToStrokeColor();
    }

    /// <summary>
    /// Gets or sets the current pen thickness.
    /// </summary>
    public float PenThickness
    {
        get => _inkProcessor.PenThickness;
        set => _inkProcessor.PenThickness = value;
    }

    /// <summary>
    /// Gets or sets the current tool mode.
    /// </summary>
    public ToolMode Mode { get; set; } = ToolMode.Pen;

    /// <summary>
    /// Gets or sets the eraser tolerance in world units.
    /// </summary>
    public float EraserTolerance { get; set; } = 10f;

    /// <summary>
    /// Raised when a stroke is completed.
    /// </summary>
    public event EventHandler<Stroke>? StrokeCompleted;

    /// <summary>
    /// Raised when erasing is requested at a position.
    /// </summary>
    public event EventHandler<(Vector2 Position, float Tolerance)>? StrokeEraseRequested;

    /// <summary>
    /// Raised when a pan gesture occurs (delta in screen pixels).
    /// </summary>
    public event EventHandler<Vector2>? PanDelta;

    /// <summary>
    /// Raised when a zoom gesture occurs.
    /// </summary>
    public event EventHandler<(float Scale, Vector2 Center)>? ZoomDelta;

    /// <summary>
    /// Raised when a redraw is needed during active drawing.
    /// </summary>
    public event EventHandler? RedrawRequested;

    /// <summary>
    /// Gets the stroke currently being drawn, if any.
    /// </summary>
    public Stroke? CurrentStroke => _inkProcessor.CurrentStroke;

    /// <summary>
    /// Gets whether a stroke is currently being drawn.
    /// </summary>
    public bool IsDrawing => _inkProcessor.IsDrawing;

    /// <summary>
    /// Gets whether the eraser is currently active.
    /// </summary>
    public bool IsErasing => _isErasing;

    /// <summary>
    /// Creates a new InkInputHandler.
    /// </summary>
    /// <param name="loggerFactory">Optional logger factory for diagnostics.</param>
    public InkInputHandler(ILoggerFactory? loggerFactory = null)
    {
        _logger = loggerFactory?.CreateLogger<InkInputHandler>();

        // Create viewport adapter that reads from our Viewport property
        _viewportAdapter = new ViewportStateAdapter(() => _viewport ?? ViewportState.Default);

        // Create the ink processor with our viewport adapter
        _inkProcessor = new InkInputProcessor(_viewportAdapter);

        // Forward events from the processor
        _inkProcessor.StrokeCompleted += (_, e) => StrokeCompleted?.Invoke(this, e.Stroke);
        _inkProcessor.RedrawRequested += (_, _) => RedrawRequested?.Invoke(this, EventArgs.Empty);
    }

    #region Pointer Events

    /// <summary>
    /// Handles pointer entered events to initialize haptic feedback for pens.
    /// </summary>
    public void OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (e.Pointer.PointerDeviceType != PointerDeviceType.Pen)
            return;

        TryInitializeHaptics(e);
    }

    /// <summary>
    /// Handles pointer exited events to stop haptic feedback.
    /// </summary>
    public void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (e.Pointer.PointerDeviceType != PointerDeviceType.Pen)
            return;

        StopHapticFeedback();
    }

    /// <summary>
    /// Handles pointer pressed events.
    /// </summary>
    public void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not UIElement element) return;
        _element = element;

        var pointer = e.Pointer;
        var point = e.GetCurrentPoint(element);
        var deviceType = pointer.PointerDeviceType;

        _logger?.LogTrace("PointerPressed: DeviceType={DeviceType}, Id={PointerId}", deviceType, pointer.PointerId);

        // Track touch contacts for multi-touch detection
        if (deviceType == PointerDeviceType.Touch)
        {
            _activeTouchPointers.Add(pointer.PointerId);

            if (_activeTouchPointers.Count > 1 && _inkProcessor.IsDrawing)
            {
                _inkProcessor.CancelStroke();
                element.ReleasePointerCapture(pointer);
            }

            e.Handled = false;
            return;
        }

        // Pen input - handle drawing/erasing
        if (deviceType == PointerDeviceType.Pen)
        {
            var action = GetPenAction(Mode, point.Properties);

            if (action == PointerAction.Draw)
            {
                var position = new Vector2((float)point.Position.X, (float)point.Position.Y);
                var props = point.Properties;
                var input = InputEventData.FromPenWithTilt(position, props.Pressure, props.XTilt, props.YTilt, pointer.PointerId);

                if (_inkProcessor.BeginStroke(input))
                {
                    StartHapticFeedback();
                    element.CapturePointer(pointer);
                    e.Handled = true;
                }
            }
            else if (action == PointerAction.Erase)
            {
                _isErasing = true;
                _eraserPointerId = pointer.PointerId;
                element.CapturePointer(pointer);

                var position = new Vector2((float)point.Position.X, (float)point.Position.Y);
                var worldPosition = _viewportAdapter.ScreenToWorld(position);
                EraseAtPosition(worldPosition);

                e.Handled = true;
            }
            else if (action == PointerAction.Pan)
            {
                e.Handled = false;
            }
            return;
        }

        // Mouse input - don't capture to allow XAML manipulation events
        if (deviceType == PointerDeviceType.Mouse)
        {
            e.Handled = false;
            return;
        }

        e.Handled = false;
    }

    /// <summary>
    /// Handles pointer moved events.
    /// </summary>
    public void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not UIElement element) return;

        var pointer = e.Pointer;
        var deviceType = pointer.PointerDeviceType;

        // Handle pen drawing movement
        if (_inkProcessor.IsDrawing && deviceType == PointerDeviceType.Pen)
        {
            var points = e.GetIntermediatePoints(element);

            bool anyPointAdded = false;
            for (int i = points.Count - 1; i >= 0; i--)
            {
                var point = points[i];
                var position = new Vector2((float)point.Position.X, (float)point.Position.Y);
                var props = point.Properties;
                var input = InputEventData.FromPenWithTilt(position, props.Pressure, props.XTilt, props.YTilt, pointer.PointerId);

                if (_inkProcessor.ContinueStroke(input, suppressRedraw: true))
                {
                    anyPointAdded = true;
                }
            }

            if (anyPointAdded)
            {
                RedrawRequested?.Invoke(this, EventArgs.Empty);
            }

            e.Handled = true;
            return;
        }

        // Handle pen erasing movement
        if (_isErasing && deviceType == PointerDeviceType.Pen && pointer.PointerId == _eraserPointerId)
        {
            var points = e.GetIntermediatePoints(element);

            foreach (var point in points)
            {
                var position = new Vector2((float)point.Position.X, (float)point.Position.Y);
                var worldPosition = _viewportAdapter.ScreenToWorld(position);
                EraseAtPosition(worldPosition);
            }

            e.Handled = true;
            return;
        }
    }

    /// <summary>
    /// Handles pointer released events.
    /// </summary>
    public void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not UIElement element) return;

        var pointer = e.Pointer;
        var deviceType = pointer.PointerDeviceType;

        if (deviceType == PointerDeviceType.Touch)
        {
            _activeTouchPointers.Remove(pointer.PointerId);
        }

        if (_inkProcessor.IsDrawing && deviceType == PointerDeviceType.Pen)
        {
            _inkProcessor.EndStroke();
            StopHapticFeedback();
            element.ReleasePointerCapture(pointer);
            e.Handled = true;
            return;
        }

        if (_isErasing && deviceType == PointerDeviceType.Pen && pointer.PointerId == _eraserPointerId)
        {
            _isErasing = false;
            _eraserPointerId = null;
            element.ReleasePointerCapture(pointer);
            e.Handled = true;
            return;
        }
    }

    /// <summary>
    /// Handles pointer cancelled events.
    /// </summary>
    public void OnPointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not UIElement element) return;

        var pointer = e.Pointer;

        if (pointer.PointerDeviceType == PointerDeviceType.Touch)
        {
            _activeTouchPointers.Remove(pointer.PointerId);
        }

        if (_inkProcessor.IsDrawing)
        {
            _inkProcessor.CancelStroke();
            element.ReleasePointerCapture(pointer);
        }
    }

    /// <summary>
    /// Handles mouse wheel events for zooming and panning.
    /// </summary>
    public void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not UIElement element) return;

        var point = e.GetCurrentPoint(element);
        var props = point.Properties;
        var center = new Vector2((float)point.Position.X, (float)point.Position.Y);
        var delta = props.MouseWheelDelta;

        if (delta == 0)
        {
            e.Handled = true;
            return;
        }

        var keyModifiers = e.KeyModifiers;

        // Ctrl+Wheel = Zoom
        if (keyModifiers.HasFlag(Windows.System.VirtualKeyModifiers.Control))
        {
            float scaleFactor = delta > 0 ? 1.1f : 0.9f;
            ZoomDelta?.Invoke(this, (scaleFactor, center));
            e.Handled = true;
            return;
        }

        float panAmount = delta / 2f;
        Vector2 panVector;

        if (props.IsHorizontalMouseWheel)
        {
            panVector = new Vector2(panAmount, 0);
        }
        else if (keyModifiers.HasFlag(Windows.System.VirtualKeyModifiers.Shift))
        {
            panVector = new Vector2(panAmount, 0);
        }
        else
        {
            panVector = new Vector2(0, panAmount);
        }

        PanDelta?.Invoke(this, panVector);
        e.Handled = true;
    }

    #endregion

    #region XAML Manipulation Events

    /// <summary>
    /// Handles XAML manipulation delta events for pan and zoom.
    /// </summary>
    public void OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
    {
        var inputDeviceType = e.PointerDeviceType switch
        {
            PointerDeviceType.Pen => InputDeviceType.Pen,
            PointerDeviceType.Touch => InputDeviceType.Touch,
            PointerDeviceType.Mouse => InputDeviceType.Mouse,
            _ => InputDeviceType.Unknown
        };

        if (!_inkProcessor.ShouldHandleManipulationDelta(inputDeviceType))
        {
            return;
        }

        var scale = e.Delta.Scale;
        var translation = e.Delta.Translation;
        var center = new Vector2((float)e.Position.X, (float)e.Position.Y);

        if (Math.Abs(scale - 1.0f) > 0.001f)
        {
            ZoomDelta?.Invoke(this, (scale, center));
        }

        var panVector = new Vector2((float)translation.X, (float)translation.Y);
        if (panVector != Vector2.Zero)
        {
            PanDelta?.Invoke(this, panVector);
        }

        e.Handled = true;
    }

    /// <summary>
    /// Handles XAML manipulation started events.
    /// </summary>
    public void OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
    {
        var inputDeviceType = e.PointerDeviceType switch
        {
            PointerDeviceType.Pen => InputDeviceType.Pen,
            PointerDeviceType.Touch => InputDeviceType.Touch,
            PointerDeviceType.Mouse => InputDeviceType.Mouse,
            _ => InputDeviceType.Unknown
        };

        _inkProcessor.HandleManipulationStarted(inputDeviceType);
        e.Handled = true;
    }

    /// <summary>
    /// Handles XAML manipulation completed events.
    /// </summary>
    public void OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
    {
        _inkProcessor.ClearPenManipulationSuppression();
        e.Handled = true;
    }

    /// <summary>
    /// Handles XAML manipulation inertia starting events.
    /// </summary>
    public void OnManipulationInertiaStarting(object sender, ManipulationInertiaStartingRoutedEventArgs e)
    {
        e.TranslationBehavior.DesiredDeceleration = 0.001;
        e.ExpansionBehavior.DesiredDeceleration = 0.0001;
        e.Handled = true;
    }

    #endregion

    #region Haptic Feedback

    private void TryInitializeHaptics(PointerRoutedEventArgs e)
    {
        try
        {
            var penDevice = Microsoft.UI.Input.Interop.PenDeviceInterop.FromPointerPoint(e.GetCurrentPoint(null));
            if (penDevice is null) return;

            _hapticsController = penDevice.SimpleHapticsController;
            if (_hapticsController is null) return;

            _inkingWaveform = FindInkingWaveform(_hapticsController);
        }
        catch
        {
            _hapticsController = null;
            _inkingWaveform = null;
        }
    }

    private static SimpleHapticsControllerFeedback? FindInkingWaveform(SimpleHapticsController controller)
    {
        SimpleHapticsControllerFeedback? pencilWaveform = null;
        SimpleHapticsControllerFeedback? inkWaveform = null;

        foreach (var waveform in controller.SupportedFeedback)
        {
            if (waveform.Waveform == KnownSimpleHapticsControllerWaveforms.PencilContinuous)
            {
                pencilWaveform = waveform;
            }
            else if (waveform.Waveform == KnownSimpleHapticsControllerWaveforms.InkContinuous)
            {
                inkWaveform = waveform;
            }
        }

        return pencilWaveform ?? inkWaveform;
    }

    private void StartHapticFeedback()
    {
        if (_hapticsController is null || _inkingWaveform is null) return;

        try
        {
            if (_hapticsController.IsIntensitySupported)
            {
                _hapticsController.SendHapticFeedback(_inkingWaveform, 0.5);
            }
            else
            {
                _hapticsController.SendHapticFeedback(_inkingWaveform);
            }
        }
        catch
        {
            // Ignore haptic failures
        }
    }

    private void StopHapticFeedback()
    {
        if (_hapticsController is null) return;

        try
        {
            _hapticsController.StopFeedback();
        }
        catch
        {
            // Ignore haptic failures
        }
    }

    #endregion

    #region Testing/Simulation

    /// <summary>
    /// Simulates a single stroke with the given screen-space points.
    /// Used for testing and demonstration.
    /// </summary>
    /// <param name="screenPoints">Array of (Position, Pressure) tuples in screen space.</param>
    /// <param name="color">Stroke color.</param>
    /// <param name="thickness">Stroke thickness.</param>
    public void SimulateStroke((Vector2 Position, float Pressure)[] screenPoints, Windows.UI.Color color, float thickness)
    {
        if (screenPoints.Length < 2) return;

        var savedColor = _inkProcessor.PenColor;
        var savedThickness = _inkProcessor.PenThickness;

        try
        {
            _inkProcessor.PenColor = color.ToStrokeColor();
            _inkProcessor.PenThickness = thickness;

            // Start stroke with first point (pass screen position - processor converts to world)
            var startInput = InputEventData.FromPen(screenPoints[0].Position, screenPoints[0].Pressure, 0);
            _inkProcessor.BeginStroke(startInput);

            // Add intermediate points
            for (int i = 1; i < screenPoints.Length - 1; i++)
            {
                var moveInput = InputEventData.FromPen(screenPoints[i].Position, screenPoints[i].Pressure, 0);
                _inkProcessor.ContinueStroke(moveInput, suppressRedraw: true);
            }

            // Add final point and end stroke
            var endInput = InputEventData.FromPen(screenPoints[^1].Position, screenPoints[^1].Pressure, 0);
            _inkProcessor.ContinueStroke(endInput, suppressRedraw: true);

            var stroke = _inkProcessor.EndStroke();
            if (stroke is not null)
            {
                StrokeCompleted?.Invoke(this, stroke);
            }
        }
        finally
        {
            _inkProcessor.PenColor = savedColor;
            _inkProcessor.PenThickness = savedThickness;
        }
    }

    /// <summary>
    /// Simulates multiple random strokes for testing.
    /// </summary>
    /// <param name="count">Number of strokes to generate.</param>
    /// <param name="canvasWidth">Canvas width in screen pixels.</param>
    /// <param name="canvasHeight">Canvas height in screen pixels.</param>
    public void SimulateRandomStrokes(int count, float canvasWidth, float canvasHeight)
    {
        var random = new Random();
        var colors = new[]
        {
            Windows.UI.Color.FromArgb(255, 0, 0, 0),      // Black
            Windows.UI.Color.FromArgb(255, 255, 0, 0),    // Red
            Windows.UI.Color.FromArgb(255, 0, 128, 0),    // Green
            Windows.UI.Color.FromArgb(255, 0, 0, 255)     // Blue
        };

        for (int i = 0; i < count; i++)
        {
            var color = colors[random.Next(colors.Length)];
            var thickness = 1f + (float)random.NextDouble() * 4f;

            // Generate random points in screen space
            int pointCount = 10 + random.Next(20);
            var points = new (Vector2 Position, float Pressure)[pointCount];

            // Start somewhere on canvas
            float x = (float)(random.NextDouble() * canvasWidth);
            float y = (float)(random.NextDouble() * canvasHeight);

            for (int p = 0; p < pointCount; p++)
            {
                // Random walk
                x += (float)(random.NextDouble() * 40 - 20);
                y += (float)(random.NextDouble() * 40 - 20);

                // Keep on canvas
                x = Math.Clamp(x, 0, canvasWidth);
                y = Math.Clamp(y, 0, canvasHeight);

                float pressure = 0.3f + (float)random.NextDouble() * 0.7f;
                points[p] = (new Vector2(x, y), pressure);
            }

            SimulateStroke(points, color, thickness);
        }

        _logger?.LogDebug("Simulated {Count} random strokes", count);
    }

    #endregion

    #region Helper Methods

    private static PointerAction GetPenAction(ToolMode mode, PointerPointProperties properties)
    {
        if (properties.IsBarrelButtonPressed)
        {
            return PointerAction.Pan;
        }

        if (properties.IsEraser)
        {
            return PointerAction.Erase;
        }

        return mode switch
        {
            ToolMode.Pen => PointerAction.Draw,
            ToolMode.Eraser => PointerAction.Erase,
            ToolMode.Pan => PointerAction.Pan,
            _ => PointerAction.None
        };
    }

    private void EraseAtPosition(Vector2 worldPosition)
    {
        StrokeEraseRequested?.Invoke(this, (worldPosition, EraserTolerance));
    }

    private enum PointerAction
    {
        None,
        Draw,
        Erase,
        Pan
    }

    #endregion

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _activeTouchPointers.Clear();
    }
}
