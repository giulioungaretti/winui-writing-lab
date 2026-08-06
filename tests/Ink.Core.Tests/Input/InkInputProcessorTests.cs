using System.Numerics;
using inkapp.Core.Input;
using inkapp.Core.Models;
using Xunit;

namespace inkapp.Tests.Input;

/// <summary>
/// Unit tests for InkInputProcessor.
/// Tests the platform-agnostic ink input processing logic.
/// </summary>
public class InkInputProcessorTests
{
    private readonly MockViewportProvider _viewport;
    private readonly InkInputProcessor _processor;

    public InkInputProcessorTests()
    {
        _viewport = new MockViewportProvider();
        _processor = new InkInputProcessor(_viewport);
    }

    #region Initialization Tests

    [Fact]
    public void Constructor_WithNullViewport_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new InkInputProcessor(null!));
    }

    [Fact]
    public void InitialState_IsNotDrawing()
    {
        Assert.False(_processor.IsDrawing);
    }

    [Fact]
    public void InitialState_CurrentStrokeIsNull()
    {
        Assert.Null(_processor.CurrentStroke);
    }

    [Fact]
    public void InitialState_HasDefaultPenColor()
    {
        var defaultBlack = StrokeColor.FromArgb(255, 0, 0, 0);
        Assert.Equal(defaultBlack, _processor.PenColor);
    }

    [Fact]
    public void InitialState_HasDefaultPenThickness()
    {
        Assert.Equal(2f, _processor.PenThickness);
    }

    #endregion

    #region BeginStroke Tests

    [Fact]
    public void BeginStroke_SetsIsDrawingTrue()
    {
        var input = InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1);

        _processor.BeginStroke(input);

        Assert.True(_processor.IsDrawing);
    }

    [Fact]
    public void BeginStroke_CreatesCurrentStroke()
    {
        var input = InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1);

        _processor.BeginStroke(input);

        Assert.NotNull(_processor.CurrentStroke);
    }

    [Fact]
    public void BeginStroke_ReturnsTrue()
    {
        var input = InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1);

        var result = _processor.BeginStroke(input);

        Assert.True(result);
    }

    [Fact]
    public void BeginStroke_StrokeHasOnePoint()
    {
        var input = InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1);

        _processor.BeginStroke(input);

        Assert.Equal(1, _processor.CurrentStroke!.Points.Count);
    }

    [Fact]
    public void BeginStroke_UsesPenColor()
    {
        _processor.PenColor = StrokeColor.FromArgb(255, 255, 0, 0);
        var input = InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1);

        _processor.BeginStroke(input);

        Assert.Equal(StrokeColor.FromArgb(255, 255, 0, 0), _processor.CurrentStroke!.Color);
    }

    [Fact]
    public void BeginStroke_UsesPenThickness()
    {
        _processor.PenThickness = 5f;
        var input = InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1);

        _processor.BeginStroke(input);

        Assert.Equal(5f, _processor.CurrentStroke!.Thickness);
    }

    [Fact]
    public void BeginStroke_TransformsScreenToWorldCoordinates()
    {
        _viewport.Zoom = 2f; // Screen coords are 2x world coords
        var input = InputEventData.FromPen(new Vector2(200, 100), 0.5f, pointerId: 1);

        _processor.BeginStroke(input);

        var point = _processor.CurrentStroke!.Points[0];
        Assert.Equal(100f, point.Position.X);
        Assert.Equal(50f, point.Position.Y);
    }

    [Fact]
    public void BeginStroke_WhileAlreadyDrawing_ReturnsFalse()
    {
        var input1 = InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1);
        var input2 = InputEventData.FromPen(new Vector2(200, 200), 0.5f, pointerId: 2);

        _processor.BeginStroke(input1);
        var result = _processor.BeginStroke(input2);

        Assert.False(result);
    }

    [Fact]
    public void BeginStroke_WhileAlreadyDrawing_DoesNotChangeCurrentStroke()
    {
        var input1 = InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1);
        var input2 = InputEventData.FromPen(new Vector2(200, 200), 0.5f, pointerId: 2);

        _processor.BeginStroke(input1);
        var originalStroke = _processor.CurrentStroke;
        _processor.BeginStroke(input2);

        Assert.Same(originalStroke, _processor.CurrentStroke);
    }

    [Fact]
    public void BeginStroke_RaisesRedrawRequested()
    {
        var eventRaised = false;
        _processor.RedrawRequested += (_, _) => eventRaised = true;
        var input = InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1);

        _processor.BeginStroke(input);

        Assert.True(eventRaised);
    }

    #endregion

    #region ContinueStroke Tests

    [Fact]
    public void ContinueStroke_AddsPointToCurrentStroke()
    {
        var input1 = InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1);
        var input2 = InputEventData.FromPen(new Vector2(110, 110), 0.6f, pointerId: 1);

        _processor.BeginStroke(input1);
        _processor.ContinueStroke(input2);

        Assert.Equal(2, _processor.CurrentStroke!.Points.Count);
    }

    [Fact]
    public void ContinueStroke_ReturnsTrue()
    {
        var input1 = InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1);
        var input2 = InputEventData.FromPen(new Vector2(110, 110), 0.6f, pointerId: 1);

        _processor.BeginStroke(input1);
        var result = _processor.ContinueStroke(input2);

        Assert.True(result);
    }

    [Fact]
    public void ContinueStroke_WithoutBeginStroke_ReturnsFalse()
    {
        var input = InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1);

        var result = _processor.ContinueStroke(input);

        Assert.False(result);
    }

    [Fact]
    public void ContinueStroke_WithDifferentPointerId_ReturnsFalse()
    {
        var input1 = InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1);
        var input2 = InputEventData.FromPen(new Vector2(110, 110), 0.6f, pointerId: 2);

        _processor.BeginStroke(input1);
        var result = _processor.ContinueStroke(input2);

        Assert.False(result);
    }

    [Fact]
    public void ContinueStroke_WithDifferentPointerId_DoesNotAddPoint()
    {
        var input1 = InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1);
        var input2 = InputEventData.FromPen(new Vector2(110, 110), 0.6f, pointerId: 2);

        _processor.BeginStroke(input1);
        _processor.ContinueStroke(input2);

        Assert.Equal(1, _processor.CurrentStroke!.Points.Count);
    }

    [Fact]
    public void ContinueStroke_TransformsScreenToWorldCoordinates()
    {
        _viewport.Zoom = 2f;
        var input1 = InputEventData.FromPen(new Vector2(200, 100), 0.5f, pointerId: 1);
        var input2 = InputEventData.FromPen(new Vector2(400, 200), 0.6f, pointerId: 1);

        _processor.BeginStroke(input1);
        _processor.ContinueStroke(input2);

        var point = _processor.CurrentStroke!.Points[1];
        Assert.Equal(200f, point.Position.X);
        Assert.Equal(100f, point.Position.Y);
    }

    [Fact]
    public void ContinueStroke_PreservesPressure()
    {
        var input1 = InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1);
        var input2 = InputEventData.FromPen(new Vector2(110, 110), 0.8f, pointerId: 1);

        _processor.BeginStroke(input1);
        _processor.ContinueStroke(input2);

        var point = _processor.CurrentStroke!.Points[1];
        Assert.Equal(0.8f, point.Pressure);
    }

    [Fact]
    public void ContinueStroke_PreservesTilt()
    {
        var input1 = InputEventData.FromPenWithTilt(new Vector2(100, 100), 0.5f, 0f, 0f, pointerId: 1);
        var input2 = InputEventData.FromPenWithTilt(new Vector2(110, 110), 0.6f, 15f, -10f, pointerId: 1);

        _processor.BeginStroke(input1);
        _processor.ContinueStroke(input2);

        var point = _processor.CurrentStroke!.Points[1];
        Assert.Equal(15f, point.TiltX);
        Assert.Equal(-10f, point.TiltY);
    }

    [Fact]
    public void ContinueStroke_RaisesRedrawRequested()
    {
        var eventCount = 0;
        _processor.RedrawRequested += (_, _) => eventCount++;
        var input1 = InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1);
        var input2 = InputEventData.FromPen(new Vector2(110, 110), 0.6f, pointerId: 1);

        _processor.BeginStroke(input1);
        _processor.ContinueStroke(input2);

        Assert.Equal(2, eventCount); // Once for begin, once for continue
    }

    #endregion

    #region EndStroke Tests

    [Fact]
    public void EndStroke_ReturnsCompletedStroke()
    {
        var input = InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1);
        _processor.BeginStroke(input);

        var stroke = _processor.EndStroke();

        Assert.NotNull(stroke);
        Assert.Equal(1, stroke!.Points.Count);
    }

    [Fact]
    public void EndStroke_SetsIsDrawingFalse()
    {
        var input = InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1);
        _processor.BeginStroke(input);

        _processor.EndStroke();

        Assert.False(_processor.IsDrawing);
    }

    [Fact]
    public void EndStroke_SetsCurrentStrokeNull()
    {
        var input = InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1);
        _processor.BeginStroke(input);

        _processor.EndStroke();

        Assert.Null(_processor.CurrentStroke);
    }

    [Fact]
    public void EndStroke_WithoutBeginStroke_ReturnsNull()
    {
        var stroke = _processor.EndStroke();

        Assert.Null(stroke);
    }

    [Fact]
    public void EndStroke_RaisesStrokeCompletedEvent()
    {
        Stroke? completedStroke = null;
        _processor.StrokeCompleted += (_, e) => completedStroke = e.Stroke;
        var input = InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1);
        _processor.BeginStroke(input);

        _processor.EndStroke();

        Assert.NotNull(completedStroke);
    }

    [Fact]
    public void EndStroke_StrokeCompletedEvent_ContainsCorrectStroke()
    {
        Stroke? completedStroke = null;
        _processor.StrokeCompleted += (_, e) => completedStroke = e.Stroke;
        var input1 = InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1);
        var input2 = InputEventData.FromPen(new Vector2(110, 110), 0.6f, pointerId: 1);
        _processor.BeginStroke(input1);
        _processor.ContinueStroke(input2);

        _processor.EndStroke();

        Assert.Equal(2, completedStroke!.Points.Count);
    }

    #endregion

    #region CancelStroke Tests

    [Fact]
    public void CancelStroke_SetsIsDrawingFalse()
    {
        var input = InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1);
        _processor.BeginStroke(input);

        _processor.CancelStroke();

        Assert.False(_processor.IsDrawing);
    }

    [Fact]
    public void CancelStroke_SetsCurrentStrokeNull()
    {
        var input = InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1);
        _processor.BeginStroke(input);

        _processor.CancelStroke();

        Assert.Null(_processor.CurrentStroke);
    }

    [Fact]
    public void CancelStroke_DoesNotRaiseStrokeCompletedEvent()
    {
        var eventRaised = false;
        _processor.StrokeCompleted += (_, _) => eventRaised = true;
        var input = InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1);
        _processor.BeginStroke(input);

        _processor.CancelStroke();

        Assert.False(eventRaised);
    }

    [Fact]
    public void CancelStroke_RaisesRedrawRequested()
    {
        var eventCount = 0;
        _processor.RedrawRequested += (_, _) => eventCount++;
        var input = InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1);
        _processor.BeginStroke(input);
        eventCount = 0; // Reset after begin

        _processor.CancelStroke();

        Assert.Equal(1, eventCount);
    }

    #endregion

    #region Multi-Point Stroke Tests (Bug Reproduction Scenarios)

    /// <summary>
    /// Tests that a complete pen stroke with multiple move events records all points.
    /// This is the expected behavior - a stroke should capture all intermediate points.
    /// </summary>
    [Fact]
    public void PenStroke_RecordsAllMoveEvents()
    {
        // Simulate a pen drawing from (100,100) to (200,200) with intermediate points
        var inputs = new[]
        {
            InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1),
            InputEventData.FromPen(new Vector2(120, 120), 0.6f, pointerId: 1),
            InputEventData.FromPen(new Vector2(140, 140), 0.7f, pointerId: 1),
            InputEventData.FromPen(new Vector2(160, 160), 0.8f, pointerId: 1),
            InputEventData.FromPen(new Vector2(180, 180), 0.7f, pointerId: 1),
            InputEventData.FromPen(new Vector2(200, 200), 0.5f, pointerId: 1),
        };

        _processor.BeginStroke(inputs[0]);
        for (int i = 1; i < inputs.Length; i++)
        {
            _processor.ContinueStroke(inputs[i]);
        }
        var stroke = _processor.EndStroke();

        Assert.NotNull(stroke);
        Assert.Equal(6, stroke!.Points.Count);
    }

    /// <summary>
    /// Tests that canceling a stroke after begin results in no stroke completion.
    /// This simulates the bug scenario where manipulation events interrupt pen input.
    /// </summary>
    [Fact]
    public void PenStroke_CanceledAfterBegin_LosesAllPoints()
    {
        Stroke? completedStroke = null;
        _processor.StrokeCompleted += (_, e) => completedStroke = e.Stroke;

        // Pen down
        var input1 = InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1);
        _processor.BeginStroke(input1);

        // Simulates manipulation event interrupting pen input
        _processor.CancelStroke();

        // Pen continues moving (but processor no longer tracking)
        var input2 = InputEventData.FromPen(new Vector2(120, 120), 0.6f, pointerId: 1);
        var continued = _processor.ContinueStroke(input2);

        Assert.False(continued); // ContinueStroke fails after cancel
        Assert.Null(completedStroke); // No stroke was completed
    }

    /// <summary>
    /// Tests the scenario where only one point is registered.
    /// This reproduces the bug: pen down, immediate cancel, pen up.
    /// </summary>
    [Fact]
    public void PenStroke_CanceledImmediately_ProducesNoStroke()
    {
        Stroke? completedStroke = null;
        _processor.StrokeCompleted += (_, e) => completedStroke = e.Stroke;

        // Pen down
        var input = InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1);
        _processor.BeginStroke(input);

        // Manipulation event cancels stroke immediately
        _processor.CancelStroke();

        // Pen up (but processor already cancelled)
        var stroke = _processor.EndStroke();

        Assert.Null(stroke); // No stroke returned from EndStroke
        Assert.Null(completedStroke); // No event raised
    }

    /// <summary>
    /// Tests that interleaved cancel/begin properly resets state.
    /// This simulates the scenario where user touches screen during pen input.
    /// </summary>
    [Fact]
    public void PenStroke_CancelAndRestart_CreatesNewStroke()
    {
        var strokes = new List<Stroke>();
        _processor.StrokeCompleted += (_, e) => strokes.Add(e.Stroke);

        // First stroke attempt
        _processor.BeginStroke(InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1));
        _processor.ContinueStroke(InputEventData.FromPen(new Vector2(110, 110), 0.5f, pointerId: 1));
        
        // Cancel (simulates manipulation start)
        _processor.CancelStroke();

        // Second stroke attempt (user lifts finger, tries again)
        _processor.BeginStroke(InputEventData.FromPen(new Vector2(200, 200), 0.5f, pointerId: 1));
        _processor.ContinueStroke(InputEventData.FromPen(new Vector2(210, 210), 0.5f, pointerId: 1));
        _processor.ContinueStroke(InputEventData.FromPen(new Vector2(220, 220), 0.5f, pointerId: 1));
        _processor.EndStroke();

        Assert.Single(strokes); // Only the second stroke completed
        Assert.Equal(3, strokes[0].Points.Count);
    }

    /// <summary>
    /// Tests that rapid pen input without interruption produces complete stroke.
    /// This is the baseline for proper behavior.
    /// </summary>
    [Fact]
    public void PenStroke_RapidInput_RecordsAllPoints()
    {
        const int pointCount = 100;
        var inputs = new InputEventData[pointCount];
        
        for (int i = 0; i < pointCount; i++)
        {
            inputs[i] = InputEventData.FromPen(
                new Vector2(100 + i, 100 + i), 
                0.3f + (i / (float)pointCount) * 0.4f,
                pointerId: 1);
        }

        _processor.BeginStroke(inputs[0]);
        for (int i = 1; i < pointCount; i++)
        {
            _processor.ContinueStroke(inputs[i]);
        }
        var stroke = _processor.EndStroke();

        Assert.NotNull(stroke);
        Assert.Equal(pointCount, stroke!.Points.Count);
    }

    /// <summary>
    /// Tests that touch input from different pointer does not interfere with pen stroke.
    /// This validates that pointer ID filtering works correctly.
    /// </summary>
    [Fact]
    public void PenStroke_IgnoresTouchInputFromDifferentPointer()
    {
        // Pen down
        _processor.BeginStroke(InputEventData.FromPen(new Vector2(100, 100), 0.5f, pointerId: 1));
        
        // Touch input from different pointer (should be ignored)
        var touchInput = new InputEventData(
            new Vector2(300, 300),
            0.5f, 0f, 0f,
            InputDeviceType.Touch,
            2,  // pointerId
            DateTime.UtcNow.Ticks);
        var touchAccepted = _processor.ContinueStroke(touchInput);
        
        // Pen continues
        _processor.ContinueStroke(InputEventData.FromPen(new Vector2(120, 120), 0.6f, pointerId: 1));
        var stroke = _processor.EndStroke();

        Assert.False(touchAccepted);
        Assert.Equal(2, stroke!.Points.Count); // Only pen points
    }

    #endregion

    #region Pressure and Tilt Tests

    [Fact]
    public void StrokePoints_PreservePressureVariation()
    {
        var pressures = new[] { 0.1f, 0.3f, 0.5f, 0.7f, 0.9f };
        
        _processor.BeginStroke(InputEventData.FromPen(new Vector2(100, 100), pressures[0], pointerId: 1));
        for (int i = 1; i < pressures.Length; i++)
        {
            _processor.ContinueStroke(InputEventData.FromPen(
                new Vector2(100 + i * 10, 100 + i * 10), 
                pressures[i], 
                pointerId: 1));
        }
        var stroke = _processor.EndStroke();

        for (int i = 0; i < pressures.Length; i++)
        {
            Assert.Equal(pressures[i], stroke!.Points[i].Pressure);
        }
    }

    [Fact]
    public void StrokePoints_PreserveTiltValues()
    {
        _processor.BeginStroke(InputEventData.FromPenWithTilt(
            new Vector2(100, 100), 0.5f, 10f, -5f, pointerId: 1));
        _processor.ContinueStroke(InputEventData.FromPenWithTilt(
            new Vector2(120, 120), 0.6f, 15f, -10f, pointerId: 1));
        _processor.ContinueStroke(InputEventData.FromPenWithTilt(
            new Vector2(140, 140), 0.7f, 20f, -15f, pointerId: 1));
        var stroke = _processor.EndStroke();

        Assert.Equal(10f, stroke!.Points[0].TiltX);
        Assert.Equal(-5f, stroke.Points[0].TiltY);
        Assert.Equal(15f, stroke.Points[1].TiltX);
        Assert.Equal(-10f, stroke.Points[1].TiltY);
        Assert.Equal(20f, stroke.Points[2].TiltX);
        Assert.Equal(-15f, stroke.Points[2].TiltY);
    }

    #endregion
}
