using inkapp.Core.Models;
using Xunit;

namespace inkapp.Tests.Models;

/// <summary>
/// Tests for BoundingRect geometry operations.
/// 
/// BoundingRect is a platform-agnostic rectangle type used for:
/// - Stroke bounding calculations
/// - Viewport intersection tests
/// - Hit-testing geometry
/// </summary>
public class BoundingRectTests
{
    #region Construction

    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var rect = new BoundingRect(10, 20, 100, 50);

        Assert.Equal(10, rect.X);
        Assert.Equal(20, rect.Y);
        Assert.Equal(100, rect.Width);
        Assert.Equal(50, rect.Height);
    }

    [Fact]
    public void EdgeProperties_CalculatedFromPositionAndSize()
    {
        var rect = new BoundingRect(10, 20, 100, 50);

        Assert.Equal(10, rect.Left);
        Assert.Equal(20, rect.Top);
        Assert.Equal(110, rect.Right);  // X + Width
        Assert.Equal(70, rect.Bottom);  // Y + Height
    }

    [Fact]
    public void FromLTRB_CreatesRectFromEdges()
    {
        var rect = BoundingRect.FromLTRB(10, 20, 110, 70);

        Assert.Equal(10, rect.X);
        Assert.Equal(20, rect.Y);
        Assert.Equal(100, rect.Width);
        Assert.Equal(50, rect.Height);
    }

    #endregion

    #region Empty Detection

    [Theory]
    [InlineData(0, 0, 0, 0)]     // Zero dimensions
    [InlineData(10, 20, 0, 50)]  // Zero width
    [InlineData(10, 20, 100, 0)] // Zero height
    [InlineData(10, 20, -5, 50)] // Negative width
    public void IsEmpty_TrueForInvalidDimensions(float x, float y, float w, float h)
    {
        var rect = new BoundingRect(x, y, w, h);
        Assert.True(rect.IsEmpty);
    }

    [Fact]
    public void IsEmpty_FalseForValidRect()
    {
        Assert.False(new BoundingRect(0, 0, 1, 1).IsEmpty);
        Assert.False(new BoundingRect(10, 20, 100, 50).IsEmpty);
    }

    [Fact]
    public void Empty_ReturnsEmptyRect()
    {
        Assert.True(BoundingRect.Empty.IsEmpty);
    }

    #endregion

    #region Intersection Testing

    [Fact]
    public void IntersectsWith_TrueForOverlappingRects()
    {
        var rect1 = new BoundingRect(0, 0, 100, 100);
        var rect2 = new BoundingRect(50, 50, 100, 100);

        Assert.True(rect1.IntersectsWith(rect2));
        Assert.True(rect2.IntersectsWith(rect1));
    }

    [Fact]
    public void IntersectsWith_FalseForSeparateRects()
    {
        var rect1 = new BoundingRect(0, 0, 100, 100);
        var rect2 = new BoundingRect(200, 200, 100, 100);

        Assert.False(rect1.IntersectsWith(rect2));
        Assert.False(rect2.IntersectsWith(rect1));
    }

    [Fact]
    public void IntersectsWith_FalseForTouchingEdges()
    {
        // Adjacent rects that share an edge but don't overlap
        var rect1 = new BoundingRect(0, 0, 100, 100);
        var rect2 = new BoundingRect(100, 0, 100, 100);

        Assert.False(rect1.IntersectsWith(rect2));
    }

    [Fact]
    public void IntersectsWith_TrueWhenOneContainsOther()
    {
        var outer = new BoundingRect(0, 0, 100, 100);
        var inner = new BoundingRect(25, 25, 50, 50);

        Assert.True(outer.IntersectsWith(inner));
        Assert.True(inner.IntersectsWith(outer));
    }

    #endregion

    #region Point Containment

    [Fact]
    public void Contains_TrueForInteriorPoint()
    {
        var rect = new BoundingRect(0, 0, 100, 100);

        Assert.True(rect.Contains(50, 50));
    }

    [Fact]
    public void Contains_TrueForBoundaryPoints()
    {
        var rect = new BoundingRect(0, 0, 100, 100);

        Assert.True(rect.Contains(0, 0));      // Top-left
        Assert.True(rect.Contains(100, 100));  // Bottom-right
        Assert.True(rect.Contains(50, 0));     // Top edge
        Assert.True(rect.Contains(0, 50));     // Left edge
    }

    [Fact]
    public void Contains_FalseForExteriorPoints()
    {
        var rect = new BoundingRect(0, 0, 100, 100);

        Assert.False(rect.Contains(-1, 50));   // Left of rect
        Assert.False(rect.Contains(101, 50));  // Right of rect
        Assert.False(rect.Contains(50, -1));   // Above rect
        Assert.False(rect.Contains(50, 101));  // Below rect
    }

    #endregion

    #region Inflate

    [Fact]
    public void Inflate_ExpandsRectOnAllSides()
    {
        var rect = new BoundingRect(10, 20, 100, 50);
        
        var inflated = rect.Inflate(5);

        Assert.Equal(5, inflated.X);      // 10 - 5
        Assert.Equal(15, inflated.Y);     // 20 - 5
        Assert.Equal(110, inflated.Width);  // 100 + 10
        Assert.Equal(60, inflated.Height);  // 50 + 10
    }

    [Fact]
    public void Inflate_WithNegativeValue_ShrinksRect()
    {
        var rect = new BoundingRect(0, 0, 100, 100);
        
        var deflated = rect.Inflate(-10);

        Assert.Equal(10, deflated.X);
        Assert.Equal(10, deflated.Y);
        Assert.Equal(80, deflated.Width);
        Assert.Equal(80, deflated.Height);
    }

    #endregion

    #region Equality

    [Fact]
    public void Equality_TrueForSameValues()
    {
        var rect1 = new BoundingRect(10, 20, 100, 50);
        var rect2 = new BoundingRect(10, 20, 100, 50);

        Assert.Equal(rect1, rect2);
    }

    [Fact]
    public void Equality_FalseForDifferentValues()
    {
        var rect1 = new BoundingRect(10, 20, 100, 50);
        var rect2 = new BoundingRect(10, 20, 100, 51);

        Assert.NotEqual(rect1, rect2);
    }

    #endregion
}
