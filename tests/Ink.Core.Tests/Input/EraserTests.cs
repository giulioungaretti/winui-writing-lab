using System.Numerics;
using inkapp.Core.Input;
using inkapp.Core.Models;
using Xunit;

namespace inkapp.Tests.Input;

/// <summary>
/// Tests for eraser functionality.
/// </summary>
public class EraserTests
{
    private readonly MockViewportProvider _viewport;

    public EraserTests()
    {
        _viewport = new MockViewportProvider();
    }

    /// <summary>
    /// Creates a stroke collection with test strokes for eraser testing.
    /// </summary>
    private static StrokeCollection CreateTestStrokes()
    {
        var collection = new StrokeCollection();
        
        // Create a horizontal stroke from (100, 100) to (200, 100)
        var stroke1 = new Stroke(StrokeColor.FromArgb(255, 0, 0, 0), 2f);
        stroke1.AddPoint(new StrokePoint(new Vector2(100, 100), 0.5f, 0f, 0f, 0));
        stroke1.AddPoint(new StrokePoint(new Vector2(150, 100), 0.5f, 0f, 0f, 1));
        stroke1.AddPoint(new StrokePoint(new Vector2(200, 100), 0.5f, 0f, 0f, 2));
        collection.Add(stroke1);
        
        // Create a vertical stroke from (300, 50) to (300, 150)
        var stroke2 = new Stroke(StrokeColor.FromArgb(255, 0, 0, 0), 2f);
        stroke2.AddPoint(new StrokePoint(new Vector2(300, 50), 0.5f, 0f, 0f, 0));
        stroke2.AddPoint(new StrokePoint(new Vector2(300, 100), 0.5f, 0f, 0f, 1));
        stroke2.AddPoint(new StrokePoint(new Vector2(300, 150), 0.5f, 0f, 0f, 2));
        collection.Add(stroke2);
        
        return collection;
    }

    #region Eraser Hit Detection Tests

    /// <summary>
    /// Test - Eraser should detect strokes at the erase position.
    /// </summary>
    [Fact]
    public void Eraser_ShouldDetectStrokeAtPosition()
    {
        var strokes = CreateTestStrokes();
        
        // Position the eraser on top of stroke1 (at 150, 100)
        var eraserPosition = new Vector2(150, 100);
        float eraserRadius = 10f;
        
        // Find strokes that intersect with the eraser
        var hitStrokes = strokes.GetStrokesAt(eraserPosition, eraserRadius);
        
        Assert.Single(hitStrokes);
    }

    /// <summary>
    /// Test - Eraser should not detect strokes when not touching any.
    /// </summary>
    [Fact]
    public void Eraser_ShouldNotDetectStrokeWhenNotTouching()
    {
        var strokes = CreateTestStrokes();
        
        // Position the eraser away from any strokes
        var eraserPosition = new Vector2(500, 500);
        float eraserRadius = 10f;
        
        // Find strokes that intersect with the eraser
        var hitStrokes = strokes.GetStrokesAt(eraserPosition, eraserRadius);
        
        Assert.Empty(hitStrokes);
    }

    /// <summary>
    /// Test - Eraser should detect multiple strokes when overlapping.
    /// </summary>
    [Fact]
    public void Eraser_ShouldDetectMultipleStrokesWhenOverlapping()
    {
        var collection = new StrokeCollection();
        
        // Create two strokes that cross at (100, 100)
        var stroke1 = new Stroke(StrokeColor.FromArgb(255, 0, 0, 0), 2f);
        stroke1.AddPoint(new StrokePoint(new Vector2(50, 100), 0.5f, 0f, 0f, 0));
        stroke1.AddPoint(new StrokePoint(new Vector2(150, 100), 0.5f, 0f, 0f, 1));
        collection.Add(stroke1);
        
        var stroke2 = new Stroke(StrokeColor.FromArgb(255, 0, 0, 0), 2f);
        stroke2.AddPoint(new StrokePoint(new Vector2(100, 50), 0.5f, 0f, 0f, 0));
        stroke2.AddPoint(new StrokePoint(new Vector2(100, 150), 0.5f, 0f, 0f, 1));
        collection.Add(stroke2);
        
        // Position the eraser at the intersection
        var eraserPosition = new Vector2(100, 100);
        float eraserRadius = 10f;
        
        var hitStrokes = collection.GetStrokesAt(eraserPosition, eraserRadius);
        
        Assert.Equal(2, hitStrokes.Count());
    }

    #endregion
}
