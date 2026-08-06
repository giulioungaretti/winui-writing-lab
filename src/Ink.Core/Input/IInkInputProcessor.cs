using inkapp.Core.Models;

namespace inkapp.Core.Input;

/// <summary>
/// Event arguments for stroke completion.
/// </summary>
public class StrokeCompletedEventArgs : EventArgs
{
    /// <summary>The completed stroke.</summary>
    public Stroke Stroke { get; }
    
    /// <summary>Creates stroke completed event arguments.</summary>
    public StrokeCompletedEventArgs(Stroke stroke)
    {
        Stroke = stroke;
    }
}

/// <summary>
/// Processes ink input events and manages stroke state.
/// This interface enables unit testing of ink input logic without UI dependencies.
/// </summary>
public interface IInkInputProcessor
{
    /// <summary>
    /// Gets or sets the current pen color for new strokes.
    /// </summary>
    StrokeColor PenColor { get; set; }
    
    /// <summary>
    /// Gets or sets the current pen thickness for new strokes.
    /// </summary>
    float PenThickness { get; set; }
    
    /// <summary>
    /// Gets whether a stroke is currently being drawn.
    /// </summary>
    bool IsDrawing { get; }
    
    /// <summary>
    /// Gets the current stroke being drawn, if any.
    /// </summary>
    Stroke? CurrentStroke { get; }
    
    
    /// <summary>
    /// Begins a new stroke at the given input position.
    /// </summary>
    /// <param name="input">Input event data for the start of the stroke.</param>
    /// <returns>True if the stroke was started successfully.</returns>
    bool BeginStroke(InputEventData input);
    
    /// <summary>
    /// Continues the current stroke with a new point.
    /// </summary>
    /// <param name="input">Input event data for the continuation point.</param>
    /// <param name="suppressRedraw">If true, does not raise RedrawRequested. Use when batching multiple points.</param>
    /// <returns>True if the point was added successfully.</returns>
    bool ContinueStroke(InputEventData input, bool suppressRedraw = false);
    
    /// <summary>
    /// Ends the current stroke.
    /// </summary>
    /// <returns>The completed stroke, or null if no stroke was in progress.</returns>
    Stroke? EndStroke();
    
    
    /// <summary>
    /// Cancels the current stroke without completing it.
    /// </summary>
    void CancelStroke();

    /// <summary>
    /// Handles manipulation started events from the UI layer.
    /// This is called when XAML fires ManipulationStarted (e.g., for touch/pen gestures).
    /// Only cancels strokes for non-pen input devices to avoid interfering with pen drawing.
    /// </summary>
    /// <param name="sourceDeviceType">The device type that triggered the manipulation.</param>
    void HandleManipulationStarted(InputDeviceType sourceDeviceType);

    /// <summary>
    /// Determines whether manipulation delta events should be handled.
    /// Returns false if pen manipulation should be suppressed (e.g., after stroke ends).
    /// </summary>
    /// <param name="sourceDeviceType">The device type that triggered the manipulation.</param>
    /// <returns>True if the manipulation should be handled, false if it should be suppressed.</returns>
    bool ShouldHandleManipulationDelta(InputDeviceType sourceDeviceType);

    /// <summary>
    /// Clears any pen manipulation suppression. Called when manipulation completes.
    /// </summary>
    void ClearPenManipulationSuppression();
    
    /// <summary>
    /// Raised when a stroke is completed.
    /// </summary>
    event EventHandler<StrokeCompletedEventArgs>? StrokeCompleted;
    
    /// <summary>
    /// Raised when the display needs to be redrawn (e.g., after adding a point).
    /// </summary>
    event EventHandler? RedrawRequested;
}
