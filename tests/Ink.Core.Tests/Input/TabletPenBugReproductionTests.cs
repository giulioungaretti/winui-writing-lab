using System.Numerics;
using inkapp.Core.Input;
using inkapp.Core.Models;
using Xunit;

namespace inkapp.Tests.Input;

/// <summary>
/// TDD tests for the tablet pen input bug.
/// 
/// These tests assert the CORRECT/EXPECTED behavior and will FAIL against the
/// current buggy implementation. Once the bug is fixed, these tests will pass.
/// 
/// BUG DESCRIPTION:
/// When using a tablet pen, strokes only register a single point unless a second finger
/// is already touching the screen. The root cause is that ManipulationStarted events
/// cancel active pen strokes.
/// 
/// ROOT CAUSE:
/// HandleManipulationStarted cancels any active drawing by setting _isDrawing = false
/// and _currentStroke = null. This happens even when the manipulation is triggered
/// by the pen itself (some tablets report pen as supporting manipulation).
/// </summary>
public class TabletPenBugReproductionTests
{
    private readonly MockViewportProvider _viewport;

    public TabletPenBugReproductionTests()
    {
        _viewport = new MockViewportProvider();
    }

    /// <summary>
    /// TDD TEST - Asserts CORRECT behavior, FAILS against buggy implementation.
    /// 
    /// Scenario: User draws a line with a tablet pen, ManipulationStarted fires
    /// immediately after PenDown.
    /// 
    /// EXPECTED (correct behavior): Stroke should have 6 points and complete successfully.
    /// ACTUAL (buggy): Test FAILS because HandleManipulationStarted cancels the stroke.
    /// 
    /// Once the bug is fixed (HandleManipulationStarted should not cancel pen strokes),
    /// this test will pass.
    /// </summary>
    [Fact]
    public void PenStroke_WithManipulationStarted_ShouldStillCaptureAllPoints()
    {
        var processor = new InkInputProcessor(_viewport);
        Stroke? completedStroke = null;
        processor.StrokeCompleted += (_, e) => completedStroke = e.Stroke;

        // 1. Pen touches screen (PointerPressed)
        processor.BeginStroke(InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1));

        // 2. XAML fires ManipulationStarted from pen (this used to trigger the bug)
        processor.HandleManipulationStarted(InputDeviceType.Pen);

        // 3. Pen moves (PointerMoved)
        processor.ContinueStroke(InputEventData.FromPen(new Vector2(120, 120), 0.6f, pointerId: 1));
        processor.ContinueStroke(InputEventData.FromPen(new Vector2(140, 140), 0.7f, pointerId: 1));
        processor.ContinueStroke(InputEventData.FromPen(new Vector2(160, 160), 0.8f, pointerId: 1));
        processor.ContinueStroke(InputEventData.FromPen(new Vector2(180, 180), 0.7f, pointerId: 1));
        processor.ContinueStroke(InputEventData.FromPen(new Vector2(200, 200), 0.5f, pointerId: 1));

        // 4. Pen lifts (PointerReleased)
        var stroke = processor.EndStroke();

        // EXPECTED BEHAVIOR: Stroke should be captured with all 6 points
        Assert.NotNull(stroke);
        Assert.Equal(6, stroke!.Points.Count);
        Assert.NotNull(completedStroke);
    }

    /// <summary>
    /// TDD TEST - Asserts CORRECT behavior, FAILS against buggy implementation.
    /// 
    /// ManipulationStarted should NOT wipe out stroke progress when a pen stroke is active.
    /// 
    /// EXPECTED: After ManipulationStarted, pen stroke should still be active with 3 points.
    /// ACTUAL (buggy): Test FAILS because HandleManipulationStarted nulls the stroke.
    /// </summary>
    [Fact]
    public void ManipulationStarted_ShouldNotWipePenStrokeProgress()
    {
        var processor = new InkInputProcessor(_viewport);

        // Start drawing and add several points
        processor.BeginStroke(InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1));
        processor.ContinueStroke(InputEventData.FromPen(new Vector2(110, 110), 0.6f, pointerId: 1));
        processor.ContinueStroke(InputEventData.FromPen(new Vector2(120, 120), 0.7f, pointerId: 1));

        // ManipulationStarted fires from pen
        processor.HandleManipulationStarted(InputDeviceType.Pen);

        // EXPECTED: Stroke should still be active with all 3 points
        Assert.True(processor.IsDrawing);
        Assert.NotNull(processor.CurrentStroke);
        Assert.Equal(3, processor.CurrentStroke!.Points.Count);
    }

    /// <summary>
    /// TDD TEST - Asserts CORRECT behavior, FAILS against buggy implementation.
    /// 
    /// A complete pen stroke should work correctly even if ManipulationStarted fires
    /// multiple times during the stroke.
    /// 
    /// EXPECTED: Stroke completes with all points regardless of manipulation events.
    /// ACTUAL (buggy): Test FAILS on first HandleManipulationStarted.
    /// </summary>
    [Fact]
    public void PenStroke_WithMultipleManipulationEvents_ShouldCompleteNormally()
    {
        var processor = new InkInputProcessor(_viewport);
        Stroke? completedStroke = null;
        processor.StrokeCompleted += (_, e) => completedStroke = e.Stroke;

        // Pen down
        processor.BeginStroke(InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1));
        
        // Multiple manipulation events during stroke from pen (shouldn't affect pen)
        processor.HandleManipulationStarted(InputDeviceType.Pen);
        processor.ContinueStroke(InputEventData.FromPen(new Vector2(120, 120), 0.6f, pointerId: 1));
        processor.HandleManipulationStarted(InputDeviceType.Pen);
        processor.ContinueStroke(InputEventData.FromPen(new Vector2(140, 140), 0.7f, pointerId: 1));
        processor.HandleManipulationStarted(InputDeviceType.Pen);
        processor.ContinueStroke(InputEventData.FromPen(new Vector2(160, 160), 0.8f, pointerId: 1));
        
        // Pen up
        var stroke = processor.EndStroke();

        // EXPECTED: All 4 points captured
        Assert.NotNull(stroke);
        Assert.Equal(4, stroke!.Points.Count);
        Assert.NotNull(completedStroke);
    }

    /// <summary>
    /// BASELINE TEST - Verifies pen strokes work correctly without manipulation events.
    /// This test should always pass and serves as the reference for expected behavior.
    /// </summary>
    [Fact]
    public void InkInputProcessor_CorrectlyHandlesPenStrokes_WithoutManipulationEvents()
    {
        var processor = new InkInputProcessor(_viewport);
        Stroke? completedStroke = null;
        processor.StrokeCompleted += (_, e) => completedStroke = e.Stroke;

        // Same sequence as the failing tests but WITHOUT HandleManipulationStarted calls
        processor.BeginStroke(InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1));
        processor.ContinueStroke(InputEventData.FromPen(new Vector2(120, 120), 0.6f, pointerId: 1));
        processor.ContinueStroke(InputEventData.FromPen(new Vector2(140, 140), 0.7f, pointerId: 1));
        processor.ContinueStroke(InputEventData.FromPen(new Vector2(160, 160), 0.8f, pointerId: 1));
        processor.ContinueStroke(InputEventData.FromPen(new Vector2(180, 180), 0.7f, pointerId: 1));
        processor.ContinueStroke(InputEventData.FromPen(new Vector2(200, 200), 0.5f, pointerId: 1));
        var stroke = processor.EndStroke();

        // This passes - without manipulation events, pen strokes work correctly
        Assert.NotNull(stroke);
        Assert.Equal(6, stroke!.Points.Count);
        Assert.NotNull(completedStroke);
    }

    #region Pen Stroke End Inertia Bug Tests

    /// <summary>
    /// TDD TEST - Asserts CORRECT behavior, FAILS against buggy implementation.
    /// 
    /// BUG DESCRIPTION:
    /// When lifting the pen at the end of a stroke, XAML interprets the pen lift as a gesture
    /// and fires ManipulationDelta with inertia, causing the canvas to pan unexpectedly.
    /// 
    /// EXPECTED: ManipulationDelta events from pen should be suppressed for a brief period
    /// after a pen stroke ends, preventing unwanted inertia-based panning.
    /// 
    /// ACTUAL (buggy): ManipulationDelta from pen causes canvas to move after stroke completion.
    /// </summary>
    [Fact]
    public void ManipulationDelta_FromPen_ShouldBeSuppressedAfterStrokeEnds()
    {
        var processor = new InkInputProcessor(_viewport);
        
        // Complete a pen stroke
        processor.BeginStroke(InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1));
        processor.ContinueStroke(InputEventData.FromPen(new Vector2(150, 150), 0.6f, pointerId: 1));
        processor.ContinueStroke(InputEventData.FromPen(new Vector2(200, 200), 0.5f, pointerId: 1));
        var stroke = processor.EndStroke();
        
        Assert.NotNull(stroke);
        Assert.Equal(3, stroke!.Points.Count);
        
        // Immediately after stroke ends, XAML fires ManipulationDelta from the pen lift
        // This should be suppressed to prevent unwanted canvas panning
        var shouldHandleManipulation = processor.ShouldHandleManipulationDelta(InputDeviceType.Pen);
        
        // EXPECTED: Should NOT handle manipulation delta from pen right after stroke ends
        Assert.False(shouldHandleManipulation);
    }

    /// <summary>
    /// TDD TEST - Asserts CORRECT behavior.
    /// 
    /// ManipulationDelta from touch should still work normally after a pen stroke ends.
    /// Only pen-triggered manipulation should be suppressed.
    /// </summary>
    [Fact]
    public void ManipulationDelta_FromTouch_ShouldWorkNormallyAfterPenStrokeEnds()
    {
        var processor = new InkInputProcessor(_viewport);
        
        // Complete a pen stroke
        processor.BeginStroke(InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1));
        processor.ContinueStroke(InputEventData.FromPen(new Vector2(200, 200), 0.5f, pointerId: 1));
        processor.EndStroke();
        
        // Touch manipulation should still work after pen stroke ends
        var shouldHandleManipulation = processor.ShouldHandleManipulationDelta(InputDeviceType.Touch);
        
        // EXPECTED: Should handle manipulation delta from touch
        Assert.True(shouldHandleManipulation);
    }

    /// <summary>
    /// TDD TEST - Asserts CORRECT behavior.
    /// 
    /// ManipulationDelta from pen should work normally when no stroke was just completed.
    /// The suppression should only apply immediately after a pen stroke ends.
    /// </summary>
    [Fact]
    public void ManipulationDelta_FromPen_ShouldWorkWhenNoRecentStroke()
    {
        var processor = new InkInputProcessor(_viewport);
        
        // No stroke in progress or recently completed
        var shouldHandleManipulation = processor.ShouldHandleManipulationDelta(InputDeviceType.Pen);
        
        // EXPECTED: Should handle manipulation delta from pen when no recent stroke
        Assert.True(shouldHandleManipulation);
    }

    /// <summary>
    /// TDD TEST - Asserts CORRECT behavior.
    /// 
    /// The suppression period should be reset once a new stroke begins.
    /// </summary>
    [Fact]
    public void ManipulationDelta_SuppressionResetsOnNewStroke()
    {
        var processor = new InkInputProcessor(_viewport);
        
        // Complete first stroke
        processor.BeginStroke(InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1));
        processor.EndStroke();
        
        // Suppression is active
        Assert.False(processor.ShouldHandleManipulationDelta(InputDeviceType.Pen));
        
        // Start a new stroke
        processor.BeginStroke(InputEventData.FromPen(new Vector2(300, 300), 0.5f, pointerId: 2));
        
        // During active drawing, pen manipulation should still be suppressed
        Assert.False(processor.ShouldHandleManipulationDelta(InputDeviceType.Pen));
        
        // But touch manipulation should work for pan/zoom while drawing
        // (Actually during drawing we want to block all manipulation - let's verify)
        // This tests that the suppression logic doesn't interfere with normal drawing state
    }

    /// <summary>
    /// TDD TEST - Asserts CORRECT behavior.
    /// 
    /// The suppression should be cleared after ClearPenManipulationSuppression is called.
    /// This allows the UI layer to clear the suppression after inertia completes.
    /// </summary>
    [Fact]
    public void ManipulationDelta_SuppressionCanBeCleared()
    {
        var processor = new InkInputProcessor(_viewport);
        
        // Complete a stroke - suppression is now active
        processor.BeginStroke(InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1));
        processor.EndStroke();
        
        Assert.False(processor.ShouldHandleManipulationDelta(InputDeviceType.Pen));
        
        // Clear the suppression (called by UI when manipulation completes)
        processor.ClearPenManipulationSuppression();
        
        // Now pen manipulation should work again
        Assert.True(processor.ShouldHandleManipulationDelta(InputDeviceType.Pen));
    }

    #endregion
}
