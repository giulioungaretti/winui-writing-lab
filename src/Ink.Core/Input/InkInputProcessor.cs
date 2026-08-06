using inkapp.Core.Models;

namespace inkapp.Core.Input;

/// <summary>
/// Processes ink input events and manages stroke state.
/// This implementation is platform-agnostic and can be unit tested.
/// </summary>
public class InkInputProcessor : IInkInputProcessor
{
    private readonly IViewportProvider _viewport;
    private Stroke? _currentStroke;
    private bool _isDrawing;
    private uint? _activePointerId;
    private bool _suppressPenManipulation;

    /// <summary>
    /// Creates a new ink input processor with the specified viewport provider.
    /// </summary>
    /// <param name="viewport">Viewport provider for coordinate transformations.</param>
    public InkInputProcessor(IViewportProvider viewport)
    {
        ArgumentNullException.ThrowIfNull(viewport);
        _viewport = viewport;
    }

    /// <inheritdoc />
    public StrokeColor PenColor { get; set; } = StrokeColor.FromArgb(255, 0, 0, 0);

    /// <inheritdoc />
    public float PenThickness { get; set; } = 2f;

    /// <inheritdoc />
    public bool IsDrawing => _isDrawing;

    /// <inheritdoc />
    public Stroke? CurrentStroke => _currentStroke;

    /// <inheritdoc />
    public event EventHandler<StrokeCompletedEventArgs>? StrokeCompleted;

    /// <inheritdoc />
    public event EventHandler? RedrawRequested;

    /// <inheritdoc />
    public bool BeginStroke(InputEventData input)
    {
        // If already drawing, don't start a new stroke (maintains single-stroke state)
        if (_isDrawing)
        {
            return false;
        }

        // Clear any lingering suppression from previous stroke
        // (but we keep suppression active during drawing to block pen manipulation)
        
        var worldPosition = _viewport.ScreenToWorld(input.Position);
        _currentStroke = new Stroke(PenColor, PenThickness);
        _currentStroke.AddPoint(new StrokePoint(
            worldPosition,
            input.Pressure,
            input.TiltX,
            input.TiltY,
            input.TimestampTicks));

        _isDrawing = true;
        _activePointerId = input.PointerId;

        RedrawRequested?.Invoke(this, EventArgs.Empty);
        return true;
    }


    /// <inheritdoc />
    public bool ContinueStroke(InputEventData input, bool suppressRedraw = false)
    {
        if (!_isDrawing || _currentStroke is null)
        {
            return false;
        }

        // Only accept input from the same pointer that started the stroke
        if (_activePointerId.HasValue && input.PointerId != _activePointerId.Value)
        {
            return false;
        }

        var worldPosition = _viewport.ScreenToWorld(input.Position);
        _currentStroke.AddPoint(new StrokePoint(
            worldPosition,
            input.Pressure,
            input.TiltX,
            input.TiltY,
            input.TimestampTicks));

        if (!suppressRedraw)
        {
            RedrawRequested?.Invoke(this, EventArgs.Empty);
        }
        return true;
    }

    /// <inheritdoc />
    public Stroke? EndStroke()
    {
        if (!_isDrawing || _currentStroke is null)
        {
            return null;
        }

        var completedStroke = _currentStroke;
        _currentStroke = null;
        _isDrawing = false;
        _activePointerId = null;

        // Suppress pen manipulation to prevent inertia after stroke ends
        _suppressPenManipulation = true;

        // Only raise event if stroke has enough points to be meaningful
        if (completedStroke.Points.Count >= 1)
        {
            StrokeCompleted?.Invoke(this, new StrokeCompletedEventArgs(completedStroke));
        }

        return completedStroke;
    }

    /// <inheritdoc />
    public void CancelStroke()
    {
        _currentStroke = null;
        _isDrawing = false;
        _activePointerId = null;

        RedrawRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void HandleManipulationStarted(InputDeviceType sourceDeviceType)
    {
        // Only cancel strokes for non-pen input devices.
        // Some tablets report pen input as supporting manipulation, causing
        // ManipulationStarted to fire immediately after pen down.
        // We should NOT cancel pen strokes - only touch/mouse manipulation should cancel drawing.
        if (sourceDeviceType == InputDeviceType.Pen)
        {
            // Pen triggered this manipulation - do not cancel the stroke
            return;
        }

        // Touch or mouse manipulation - cancel any active drawing
        // This allows multi-touch gestures to take over from single-touch drawing
        if (_isDrawing && _currentStroke is not null)
        {
            _currentStroke = null;
            _isDrawing = false;
            _activePointerId = null;
        }
    }

    /// <inheritdoc />
    public bool ShouldHandleManipulationDelta(InputDeviceType sourceDeviceType)
    {
        // During active drawing, don't handle any manipulation
        if (_isDrawing)
        {
            return false;
        }

        // After pen stroke ends, suppress pen manipulation to prevent inertia
        if (sourceDeviceType == InputDeviceType.Pen && _suppressPenManipulation)
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public void ClearPenManipulationSuppression()
    {
        _suppressPenManipulation = false;
    }
}
