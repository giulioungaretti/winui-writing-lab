using System.Numerics;
using inkapp.Core.Models;
using Xunit;

namespace inkapp.Tests.Models;

/// <summary>
/// Tests for StrokeCollection management and query operations.
/// 
/// StrokeCollection manages a page's ink strokes with:
/// - Add/Remove/Clear operations with change tracking (IsDirty flag)
/// - Change notification events for UI binding
/// - Spatial queries for viewport culling (GetVisibleStrokes)
/// - Hit testing for eraser tool (GetStrokesAt)
/// </summary>
public class StrokeCollectionTests
{
    #region Collection Management

    [Fact]
    public void NewCollection_IsEmpty()
    {
        var collection = new StrokeCollection();

        Assert.Empty(collection.Strokes);
    }

    [Fact]
    public void NewCollection_IsNotDirty()
    {
        var collection = new StrokeCollection();

        Assert.False(collection.IsDirty);
    }

    [Fact]
    public void Add_MakesStrokeAccessible()
    {
        var collection = new StrokeCollection();
        var stroke = new Stroke(StrokeColor.Black, 2f);

        collection.Add(stroke);

        Assert.Single(collection.Strokes);
        Assert.Contains(stroke, collection.Strokes);
    }

    [Fact]
    public void Add_MarksCollectionDirty()
    {
        var collection = new StrokeCollection();

        collection.Add(new Stroke(StrokeColor.Black, 2f));

        Assert.True(collection.IsDirty);
    }

    [Fact]
    public void Remove_RemovesStrokeFromCollection()
    {
        var collection = new StrokeCollection();
        var stroke = new Stroke(StrokeColor.Black, 2f);
        collection.Add(stroke);

        var removed = collection.Remove(stroke);

        Assert.True(removed);
        Assert.Empty(collection.Strokes);
    }

    [Fact]
    public void Remove_ReturnsFalse_WhenStrokeNotInCollection()
    {
        var collection = new StrokeCollection();

        var removed = collection.Remove(new Stroke(StrokeColor.Black, 2f));

        Assert.False(removed);
    }

    [Fact]
    public void Clear_RemovesAllStrokes()
    {
        var collection = new StrokeCollection();
        collection.Add(new Stroke(StrokeColor.Black, 2f));
        collection.Add(new Stroke(StrokeColor.Red, 3f));

        collection.Clear();

        Assert.Empty(collection.Strokes);
    }

    [Fact]
    public void Clear_OnEmptyCollection_DoesNotChangeDirtyFlag()
    {
        var collection = new StrokeCollection();
        
        collection.Clear();

        Assert.False(collection.IsDirty);
    }

    [Fact]
    public void MarkClean_ResetsDirtyFlag()
    {
        var collection = new StrokeCollection();
        collection.Add(new Stroke(StrokeColor.Black, 2f));

        collection.MarkClean();

        Assert.False(collection.IsDirty);
    }

    #endregion

    #region Change Notifications - For UI data binding

    [Fact]
    public void Add_RaisesStrokesChangedEvent()
    {
        var collection = new StrokeCollection();
        var eventFired = false;
        collection.StrokesChanged += (s, e) => eventFired = true;

        collection.Add(new Stroke(StrokeColor.Black, 2f));

        Assert.True(eventFired);
    }

    [Fact]
    public void Remove_RaisesStrokesChangedEvent()
    {
        var collection = new StrokeCollection();
        var stroke = new Stroke(StrokeColor.Black, 2f);
        collection.Add(stroke);
        
        var eventFired = false;
        collection.StrokesChanged += (s, e) => eventFired = true;

        collection.Remove(stroke);

        Assert.True(eventFired);
    }

    [Fact]
    public void Clear_RaisesStrokesChangedEvent()
    {
        var collection = new StrokeCollection();
        collection.Add(new Stroke(StrokeColor.Black, 2f));
        
        var eventFired = false;
        collection.StrokesChanged += (s, e) => eventFired = true;

        collection.Clear();

        Assert.True(eventFired);
    }

    [Fact]
    public void Remove_DoesNotRaiseEvent_WhenStrokeNotFound()
    {
        var collection = new StrokeCollection();
        var eventFired = false;
        collection.StrokesChanged += (s, e) => eventFired = true;

        collection.Remove(new Stroke(StrokeColor.Black, 2f));

        Assert.False(eventFired);
    }

    #endregion

    #region Viewport Queries - For rendering optimization

    [Fact]
    public void GetVisibleStrokes_ReturnsOnlyIntersectingStrokes()
    {
        var collection = new StrokeCollection();
        
        var insideStroke = CreateStroke(new Vector2(50, 50), new Vector2(60, 60));
        collection.Add(insideStroke);

        var outsideStroke = CreateStroke(new Vector2(200, 200), new Vector2(210, 210));
        collection.Add(outsideStroke);

        var viewport = new BoundingRect(0, 0, 100, 100);
        var visible = collection.GetVisibleStrokes(viewport).ToList();

        Assert.Single(visible);
        Assert.Contains(insideStroke, visible);
        Assert.DoesNotContain(outsideStroke, visible);
    }

    #endregion

    #region Hit Testing - For eraser tool

    [Fact]
    public void GetStrokesAt_ReturnsStrokesNearPoint()
    {
        var collection = new StrokeCollection();
        
        var nearStroke = CreateStroke(new Vector2(0, 50), new Vector2(100, 50), thickness: 4f);
        collection.Add(nearStroke);

        var farStroke = CreateStroke(new Vector2(0, 200), new Vector2(100, 200), thickness: 4f);
        collection.Add(farStroke);

        var strokesAtPoint = collection.GetStrokesAt(new Vector2(50, 50), tolerance: 5f).ToList();

        Assert.Single(strokesAtPoint);
        Assert.Contains(nearStroke, strokesAtPoint);
    }

    [Fact]
    public void GetStrokesAt_WithZeroTolerance_RequiresDirectHit()
    {
        var collection = new StrokeCollection();
        
        var stroke = CreateStroke(new Vector2(0, 50), new Vector2(100, 50), thickness: 4f);
        collection.Add(stroke);

        // Point on line
        var onLine = collection.GetStrokesAt(new Vector2(50, 50), tolerance: 0f).ToList();
        Assert.Single(onLine);

        // Point 10 units away (outside half-thickness of 2)
        var offLine = collection.GetStrokesAt(new Vector2(50, 60), tolerance: 0f).ToList();
        Assert.Empty(offLine);
    }

    #endregion

    #region Error Handling

    [Fact]
    public void Add_ThrowsOnNull()
    {
        var collection = new StrokeCollection();

        Assert.Throws<ArgumentNullException>(() => collection.Add(null!));
    }

    #endregion

    #region Test Helpers

    private static Stroke CreateStroke(Vector2 start, Vector2 end, float thickness = 2f)
    {
        var stroke = new Stroke(StrokeColor.Black, thickness);
        stroke.AddPoint(new StrokePoint(start, 0.5f, 0, 0, 100));
        stroke.AddPoint(new StrokePoint(end, 0.5f, 0, 0, 200));
        return stroke;
    }

    #endregion
}
