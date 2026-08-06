using System.Numerics;
using inkapp.Core.Models;
using Xunit;

namespace inkapp.Tests.Models;

/// <summary>
/// Tests for Stroke geometry and hit-testing behavior.
/// 
/// A Stroke represents a series of points drawn by the user with a pen/stylus.
/// Key behaviors tested:
/// - Bounding rectangle calculation (for efficient viewport culling)
/// - Hit testing via ContainsPoint (for eraser tool selection)
/// - Rectangle intersection (for viewport visibility and lasso selection)
/// - Stroke thickness affects hit testing and bounding calculations
/// </summary>
public class StrokeTests
{
    #region Stroke Creation

    [Fact]
    public void NewStrokes_HaveUniqueIdentifiers()
    {
        var stroke1 = new Stroke(StrokeColor.Black, 2f);
        var stroke2 = new Stroke(StrokeColor.Black, 2f);

        Assert.NotEqual(Guid.Empty, stroke1.Id);
        Assert.NotEqual(stroke1.Id, stroke2.Id);
    }

    [Fact]
    public void Stroke_CanBeCreatedWithSpecificId()
    {
        // Needed for deserialization from saved files
        var id = Guid.NewGuid();
        var stroke = new Stroke(id, StrokeColor.Red, 3f);

        Assert.Equal(id, stroke.Id);
    }

    [Fact]
    public void Stroke_CanBeCreatedWithPreexistingPoints()
    {
        // Needed for deserialization
        var id = Guid.NewGuid();
        var points = new[]
        {
            new StrokePoint(new Vector2(0, 0), 0.5f, 0, 0, 100),
            new StrokePoint(new Vector2(10, 10), 0.7f, 0, 0, 200),
            new StrokePoint(new Vector2(20, 20), 0.6f, 0, 0, 300),
        };

        var stroke = new Stroke(id, points, StrokeColor.Blue, 2f);

        Assert.Equal(3, stroke.Points.Count);
    }

    [Fact]
    public void Stroke_PreservesColorAndThickness()
    {
        var color = StrokeColor.FromArgb(128, 255, 100, 50);
        var stroke = new Stroke(color, 5.5f);

        Assert.Equal(color, stroke.Color);
        Assert.Equal(5.5f, stroke.Thickness);
    }

    #endregion

    #region Adding Points

    [Fact]
    public void AddPoint_IncreasesPointCount()
    {
        var stroke = new Stroke(StrokeColor.Black, 2f);
        
        stroke.AddPoint(new StrokePoint(new Vector2(0, 0), 0.5f, 0, 0, 100));
        Assert.Single(stroke.Points);

        stroke.AddPoint(new StrokePoint(new Vector2(10, 10), 0.5f, 0, 0, 200));
        Assert.Equal(2, stroke.Points.Count);
    }

    [Fact]
    public void AddPoint_UpdatesBoundingRect()
    {
        var stroke = new Stroke(StrokeColor.Black, 2f);
        
        stroke.AddPoint(new StrokePoint(new Vector2(10, 20), 0.5f, 0, 0, 100));
        var bounds1 = stroke.BoundingRect;

        stroke.AddPoint(new StrokePoint(new Vector2(100, 200), 0.5f, 0, 0, 200));
        var bounds2 = stroke.BoundingRect;

        Assert.True(bounds2.Width > bounds1.Width);
        Assert.True(bounds2.Height > bounds1.Height);
    }

    #endregion

    #region Bounding Rectangle - Used for viewport culling

    [Fact]
    public void BoundingRect_IsEmpty_WhenNoPoints()
    {
        var stroke = new Stroke(StrokeColor.Black, 2f);

        Assert.True(stroke.BoundingRect.IsEmpty);
    }

    [Fact]
    public void BoundingRect_ExpandsByStrokeThickness()
    {
        // Bounding rect must include the visual extent of the stroke,
        // not just the center line
        var stroke = new Stroke(StrokeColor.Black, 4f);
        stroke.AddPoint(new StrokePoint(new Vector2(10, 20), 0.5f, 0, 0, 100));

        var bounds = stroke.BoundingRect;

        // Point at (10,20) with thickness 4 should have bounds expanded by half-thickness (2)
        Assert.Equal(8, bounds.X, 0.001);   // 10 - 2
        Assert.Equal(18, bounds.Y, 0.001);  // 20 - 2
        Assert.Equal(4, bounds.Width, 0.001);
        Assert.Equal(4, bounds.Height, 0.001);
    }

    [Fact]
    public void BoundingRect_EncompassesAllPoints()
    {
        var stroke = new Stroke(StrokeColor.Black, 2f);
        stroke.AddPoint(new StrokePoint(new Vector2(0, 0), 0.5f, 0, 0, 100));
        stroke.AddPoint(new StrokePoint(new Vector2(100, 50), 0.5f, 0, 0, 200));
        stroke.AddPoint(new StrokePoint(new Vector2(50, 100), 0.5f, 0, 0, 300));

        var bounds = stroke.BoundingRect;

        // Half thickness is 1, bounds expanded accordingly
        Assert.Equal(-1, bounds.Left, 0.001);
        Assert.Equal(-1, bounds.Top, 0.001);
        Assert.Equal(101, bounds.Right, 0.001);
        Assert.Equal(101, bounds.Bottom, 0.001);
    }

    #endregion

    #region Hit Testing (ContainsPoint) - For eraser tool

    [Fact]
    public void ContainsPoint_ReturnsTrueForPointOnStroke()
    {
        var stroke = new Stroke(StrokeColor.Black, 4f);
        stroke.AddPoint(new StrokePoint(new Vector2(0, 0), 0.5f, 0, 0, 100));
        stroke.AddPoint(new StrokePoint(new Vector2(100, 0), 0.5f, 0, 0, 200));

        Assert.True(stroke.ContainsPoint(new Vector2(50, 0), 0f));
    }

    [Fact]
    public void ContainsPoint_ConsidersStrokeThickness()
    {
        var stroke = new Stroke(StrokeColor.Black, 4f);
        stroke.AddPoint(new StrokePoint(new Vector2(0, 0), 0.5f, 0, 0, 100));
        stroke.AddPoint(new StrokePoint(new Vector2(100, 0), 0.5f, 0, 0, 200));

        // Point 1.5 units away from line, within half-thickness (2)
        Assert.True(stroke.ContainsPoint(new Vector2(50, 1.5f), 0f));
    }

    [Fact]
    public void ContainsPoint_UsesTolerance_ForEraserTool()
    {
        // Eraser tool adds extra tolerance for easier selection
        var stroke = new Stroke(StrokeColor.Black, 2f);
        stroke.AddPoint(new StrokePoint(new Vector2(0, 0), 0.5f, 0, 0, 100));
        stroke.AddPoint(new StrokePoint(new Vector2(100, 0), 0.5f, 0, 0, 200));

        // Point 5 units away, but within 10 unit tolerance
        Assert.True(stroke.ContainsPoint(new Vector2(50, 5), 10f));
    }

    [Fact]
    public void ContainsPoint_ReturnsFalseForDistantPoint()
    {
        var stroke = new Stroke(StrokeColor.Black, 2f);
        stroke.AddPoint(new StrokePoint(new Vector2(0, 0), 0.5f, 0, 0, 100));
        stroke.AddPoint(new StrokePoint(new Vector2(100, 0), 0.5f, 0, 0, 200));

        Assert.False(stroke.ContainsPoint(new Vector2(50, 20), 0f));
    }

    [Fact]
    public void ContainsPoint_WorksNearEndpoints()
    {
        var stroke = new Stroke(StrokeColor.Black, 4f);
        stroke.AddPoint(new StrokePoint(new Vector2(0, 0), 0.5f, 0, 0, 100));
        stroke.AddPoint(new StrokePoint(new Vector2(100, 0), 0.5f, 0, 0, 200));

        // Points just beyond endpoints are still within stroke thickness
        Assert.True(stroke.ContainsPoint(new Vector2(-1, 0), 0f));
        Assert.True(stroke.ContainsPoint(new Vector2(101, 0), 0f));
    }

    [Fact]
    public void ContainsPoint_RequiresAtLeastTwoPoints()
    {
        // Single point strokes have no segments to test against
        var stroke = new Stroke(StrokeColor.Black, 4f);
        stroke.AddPoint(new StrokePoint(new Vector2(50, 50), 0.5f, 0, 0, 100));

        Assert.False(stroke.ContainsPoint(new Vector2(50, 50), 0f));
    }

    #endregion

    #region Rectangle Intersection - For viewport culling and lasso selection

    [Fact]
    public void Intersects_ReturnsTrueWhenLineCrossesRect()
    {
        var stroke = new Stroke(StrokeColor.Black, 2f);
        stroke.AddPoint(new StrokePoint(new Vector2(0, 50), 0.5f, 0, 0, 100));
        stroke.AddPoint(new StrokePoint(new Vector2(100, 50), 0.5f, 0, 0, 200));

        var rect = new BoundingRect(40, 40, 20, 20);

        Assert.True(stroke.Intersects(rect));
    }

    [Fact]
    public void Intersects_ReturnsTrueWhenDiagonalCrossesRect()
    {
        var stroke = new Stroke(StrokeColor.Black, 2f);
        stroke.AddPoint(new StrokePoint(new Vector2(0, 0), 0.5f, 0, 0, 100));
        stroke.AddPoint(new StrokePoint(new Vector2(100, 100), 0.5f, 0, 0, 200));

        // Rect that the diagonal clearly passes through
        var rect = new BoundingRect(40, 45, 20, 10);

        Assert.True(stroke.Intersects(rect));
    }

    [Fact]
    public void Intersects_ReturnsFalseForDistantRect()
    {
        var stroke = new Stroke(StrokeColor.Black, 2f);
        stroke.AddPoint(new StrokePoint(new Vector2(0, 0), 0.5f, 0, 0, 100));
        stroke.AddPoint(new StrokePoint(new Vector2(10, 10), 0.5f, 0, 0, 200));

        var rect = new BoundingRect(100, 100, 20, 20);

        Assert.False(stroke.Intersects(rect));
    }

    [Fact]
    public void Intersects_ReturnsTrueWhenEndpointInsideRect()
    {
        var stroke = new Stroke(StrokeColor.Black, 2f);
        stroke.AddPoint(new StrokePoint(new Vector2(0, 0), 0.5f, 0, 0, 100));
        stroke.AddPoint(new StrokePoint(new Vector2(100, 100), 0.5f, 0, 0, 200));

        var rect = new BoundingRect(-5, -5, 10, 10);

        Assert.True(stroke.Intersects(rect));
    }

    [Fact]
    public void Intersects_DetectsLShapedStrokeCorrectly()
    {
        // Tests that we check actual segments, not just bounding boxes
        var stroke = new Stroke(StrokeColor.Black, 2f);
        stroke.AddPoint(new StrokePoint(new Vector2(0, 0), 0.5f, 0, 0, 100));
        stroke.AddPoint(new StrokePoint(new Vector2(0, 100), 0.5f, 0, 0, 200));
        stroke.AddPoint(new StrokePoint(new Vector2(100, 100), 0.5f, 0, 0, 300));

        // Rect in the "empty corner" - bounding boxes overlap but no segment crosses
        var rect = new BoundingRect(50, 10, 10, 10);

        Assert.False(stroke.Intersects(rect));
    }

    [Fact]
    public void Intersects_ConsidersStrokeThickness()
    {
        var stroke = new Stroke(StrokeColor.Black, 20f);
        stroke.AddPoint(new StrokePoint(new Vector2(0, 0), 0.5f, 0, 0, 100));
        stroke.AddPoint(new StrokePoint(new Vector2(100, 0), 0.5f, 0, 0, 200));

        // Rect 5 units from line center, within half-thickness (10)
        var rect = new BoundingRect(40, 5, 20, 20);

        Assert.True(stroke.Intersects(rect));
    }

    [Fact]
    public void Intersects_RequiresAtLeastTwoPoints()
    {
        var stroke = new Stroke(StrokeColor.Black, 2f);
        stroke.AddPoint(new StrokePoint(new Vector2(50, 50), 0.5f, 0, 0, 100));
        
        var rect = new BoundingRect(40, 40, 20, 20);

        Assert.False(stroke.Intersects(rect));
    }

    [Fact]
    public void Intersects_ReturnsFalseForEmptyStroke()
    {
        var stroke = new Stroke(StrokeColor.Black, 2f);
        var rect = new BoundingRect(0, 0, 100, 100);

        Assert.False(stroke.Intersects(rect));
    }

    #endregion

    #region Property Modification

    [Fact]
    public void Color_CanBeChanged()
    {
        var stroke = new Stroke(StrokeColor.Black, 2f);
        
        stroke.Color = StrokeColor.Red;

        Assert.Equal(StrokeColor.Red, stroke.Color);
    }

    [Fact]
    public void Thickness_CanBeChanged()
    {
        var stroke = new Stroke(StrokeColor.Black, 2f);
        
        stroke.Thickness = 10f;

        Assert.Equal(10f, stroke.Thickness);
    }

    #endregion
}
