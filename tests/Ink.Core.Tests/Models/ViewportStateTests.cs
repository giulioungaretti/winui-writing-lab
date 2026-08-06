using Xunit;
using System.Numerics;
using inkapp.Core.Models;

namespace inkapp.Tests.Models;

/// <summary>
/// Tests for ViewportState coordinate transformations and zoom behavior.
/// 
/// ViewportState represents the view into an infinite canvas with pan (offset) and zoom.
/// Key behaviors tested:
/// - Screen-to-world and world-to-screen coordinate conversions
/// - Zoom operations maintain focal point stability (zoom around cursor)
/// - Zoom values are clamped to safe ranges (25% - 800%)
/// - Transform matrices match coordinate conversion methods
/// </summary>
public class ViewportStateTests
{
    private const float Tolerance = 0.0001f;

    #region Default State

    [Fact]
    public void DefaultViewport_HasNoOffset()
    {
        var viewport = ViewportState.Default;

        Assert.Equal(Vector2.Zero, viewport.Pan);
    }

    [Fact]
    public void DefaultViewport_HasNormalZoom()
    {
        var viewport = ViewportState.Default;

        Assert.Equal(1.0f, viewport.Zoom);
    }

    #endregion

    #region Coordinate Conversions - These test the core canvas navigation behavior

    [Fact]
    public void ScreenToWorld_WithDefaultViewport_CoordinatesUnchanged()
    {
        var viewport = ViewportState.Default;
        var screen = new Vector2(100, 200);

        var world = viewport.ScreenToWorld(screen);

        Assert.Equal(screen, world);
    }

    [Fact]
    public void WorldToScreen_WithDefaultViewport_CoordinatesUnchanged()
    {
        var viewport = ViewportState.Default;
        var world = new Vector2(100, 200);

        var screen = viewport.WorldToScreen(world);

        Assert.Equal(world, screen);
    }

    [Fact]
    public void ScreenToWorld_WithPannedCanvas_AccountsForOffset()
    {
        // When user pans the canvas right and down, 
        // a screen point maps to a different world location
        var viewport = new ViewportState { Pan = new Vector2(50, 100), Zoom = 1.0f };
        var screen = new Vector2(150, 200);

        var world = viewport.ScreenToWorld(screen);

        // Screen (150,200) with pan (50,100) maps to world (100,100)
        Assert.Equal(100f, world.X, Tolerance);
        Assert.Equal(100f, world.Y, Tolerance);
    }

    [Fact]
    public void WorldToScreen_WithPannedCanvas_AccountsForOffset()
    {
        var viewport = new ViewportState { Pan = new Vector2(50, 100), Zoom = 1.0f };
        var world = new Vector2(100, 100);

        var screen = viewport.WorldToScreen(world);

        // World (100,100) with pan (50,100) maps to screen (150,200)
        Assert.Equal(150f, screen.X, Tolerance);
        Assert.Equal(200f, screen.Y, Tolerance);
    }

    [Fact]
    public void ScreenToWorld_WithZoomedCanvas_ScalesCoordinates()
    {
        // When zoomed in 2x, screen pixels map to half as many world units
        var viewport = new ViewportState { Pan = Vector2.Zero, Zoom = 2.0f };
        var screen = new Vector2(200, 400);

        var world = viewport.ScreenToWorld(screen);

        Assert.Equal(100f, world.X, Tolerance);
        Assert.Equal(200f, world.Y, Tolerance);
    }

    [Fact]
    public void WorldToScreen_WithZoomedCanvas_ScalesCoordinates()
    {
        // When zoomed in 2x, world units map to twice as many screen pixels
        var viewport = new ViewportState { Pan = Vector2.Zero, Zoom = 2.0f };
        var world = new Vector2(100, 200);

        var screen = viewport.WorldToScreen(world);

        Assert.Equal(200f, screen.X, Tolerance);
        Assert.Equal(400f, screen.Y, Tolerance);
    }

    [Fact]
    public void ScreenToWorld_WithPanAndZoom_AppliesBothTransforms()
    {
        // Combined pan and zoom: common in real usage
        var viewport = new ViewportState { Pan = new Vector2(100, 50), Zoom = 2.0f };
        var screen = new Vector2(300, 250);

        var world = viewport.ScreenToWorld(screen);

        // (300,250) - pan(100,50) = (200,200), then /2 = (100,100)
        Assert.Equal(100f, world.X, Tolerance);
        Assert.Equal(100f, world.Y, Tolerance);
    }

    [Fact]
    public void WorldToScreen_WithPanAndZoom_AppliesBothTransforms()
    {
        var viewport = new ViewportState { Pan = new Vector2(100, 50), Zoom = 2.0f };
        var world = new Vector2(100, 100);

        var screen = viewport.WorldToScreen(world);

        // (100,100) * 2 = (200,200), then + pan(100,50) = (300,250)
        Assert.Equal(300f, screen.X, Tolerance);
        Assert.Equal(250f, screen.Y, Tolerance);
    }

    [Fact]
    public void CoordinateConversions_AreReversible()
    {
        // Critical for ink rendering: converting back and forth should be lossless
        var viewport = new ViewportState { Pan = new Vector2(123.5f, -45.7f), Zoom = 1.75f };
        var original = new Vector2(456.789f, -123.456f);

        var world = viewport.ScreenToWorld(original);
        var backToScreen = viewport.WorldToScreen(world);

        Assert.Equal(original.X, backToScreen.X, Tolerance);
        Assert.Equal(original.Y, backToScreen.Y, Tolerance);
    }

    #endregion

    #region Pan and Zoom Modifications

    [Fact]
    public void WithPan_CreatesNewViewportwithUpdatedPan()
    {
        var original = ViewportState.Default;
        var newPan = new Vector2(100, 200);

        var updated = original.WithPan(newPan);

        Assert.Equal(newPan, updated.Pan);
        Assert.Equal(original.Zoom, updated.Zoom);
    }

    [Fact]
    public void AddPan_AccumulatesPanOffset()
    {
        var viewport = new ViewportState { Pan = new Vector2(50, 100) };
        var delta = new Vector2(25, -30);

        var updated = viewport.AddPan(delta);

        Assert.Equal(75f, updated.Pan.X, Tolerance);
        Assert.Equal(70f, updated.Pan.Y, Tolerance);
    }

    [Fact]
    public void WithZoom_CreatesNewViewportwithUpdatedZoom()
    {
        var original = ViewportState.Default;

        var updated = original.WithZoom(2.5f);

        Assert.Equal(2.5f, updated.Zoom, Tolerance);
        Assert.Equal(original.Pan, updated.Pan);
    }

    #endregion

    #region Zoom Clamping - Prevents extreme zoom levels that hurt usability

    [Fact]
    public void WithZoom_ClampsToMinimum_WhenBelowLimit()
    {
        var viewport = ViewportState.Default;

        var updated = viewport.WithZoom(0.1f); // Below MinZoom (0.25)

        Assert.Equal(ViewportState.MinZoom, updated.Zoom, Tolerance);
    }

    [Fact]
    public void WithZoom_ClampsToMaximum_WhenAboveLimit()
    {
        var viewport = ViewportState.Default;

        var updated = viewport.WithZoom(10f); // Above MaxZoom (8.0)

        Assert.Equal(ViewportState.MaxZoom, updated.Zoom, Tolerance);
    }

    [Theory]
    [InlineData(0.25f)]  // MinZoom
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    [InlineData(2.0f)]
    [InlineData(4.0f)]
    [InlineData(8.0f)]   // MaxZoom
    public void WithZoom_AcceptsValuesWithinRange(float zoom)
    {
        var viewport = ViewportState.Default;

        var updated = viewport.WithZoom(zoom);

        Assert.Equal(zoom, updated.Zoom, Tolerance);
    }

    #endregion

    #region Zoom Around Point - Critical for intuitive zoom behavior

    [Fact]
    public void ZoomAroundPoint_KeepsPointUnderCursorStationary()
    {
        // This is the key UX behavior: when zooming with scroll wheel,
        // the point under the cursor should not move
        var viewport = new ViewportState { Pan = new Vector2(100, 100), Zoom = 1.0f };
        var cursorPosition = new Vector2(300, 300);
        
        var worldBefore = viewport.ScreenToWorld(cursorPosition);
        var zoomed = viewport.ZoomAroundPoint(2.0f, cursorPosition);
        var screenAfter = zoomed.WorldToScreen(worldBefore);

        Assert.Equal(cursorPosition.X, screenAfter.X, Tolerance);
        Assert.Equal(cursorPosition.Y, screenAfter.Y, Tolerance);
    }

    [Fact]
    public void ZoomAroundPoint_ClampsToMinimum()
    {
        var viewport = new ViewportState { Zoom = 0.5f };
        var focalPoint = new Vector2(100, 100);

        var zoomed = viewport.ZoomAroundPoint(0.1f, focalPoint);

        Assert.Equal(ViewportState.MinZoom, zoomed.Zoom, Tolerance);
    }

    [Fact]
    public void ZoomAroundPoint_ClampsToMaximum()
    {
        var viewport = new ViewportState { Zoom = 4.0f };
        var focalPoint = new Vector2(100, 100);

        var zoomed = viewport.ZoomAroundPoint(10f, focalPoint);

        Assert.Equal(ViewportState.MaxZoom, zoomed.Zoom, Tolerance);
    }

    [Fact]
    public void ZoomAroundPoint_WithNoChange_ReturnsEquivalentState()
    {
        var viewport = new ViewportState { Pan = new Vector2(50, 50), Zoom = 2.0f };
        var focalPoint = new Vector2(100, 100);

        var result = viewport.ZoomAroundPoint(2.0f, focalPoint);

        Assert.Equal(viewport.Pan, result.Pan);
        Assert.Equal(viewport.Zoom, result.Zoom);
    }

    [Theory]
    [InlineData(100, 100)]
    [InlineData(0, 0)]
    [InlineData(500, 300)]
    [InlineData(-100, 200)]
    public void ZoomAroundPoint_WorksWithAnyFocalPoint(float x, float y)
    {
        var viewport = new ViewportState { Pan = new Vector2(75, -25), Zoom = 1.5f };
        var focalPoint = new Vector2(x, y);
        
        var worldBefore = viewport.ScreenToWorld(focalPoint);
        var zoomed = viewport.ZoomAroundPoint(3.0f, focalPoint);
        var screenAfter = zoomed.WorldToScreen(worldBefore);

        Assert.Equal(focalPoint.X, screenAfter.X, Tolerance);
        Assert.Equal(focalPoint.Y, screenAfter.Y, Tolerance);
    }

    #endregion

    #region Transform Matrices - For GPU rendering compatibility

    [Fact]
    public void GetTransformMatrix_AtDefault_IsIdentity()
    {
        var viewport = ViewportState.Default;

        var matrix = viewport.GetTransformMatrix();

        Assert.Equal(Matrix3x2.Identity, matrix);
    }

    [Fact]
    public void GetTransformMatrix_ProducesSameResultAsWorldToScreen()
    {
        var viewport = new ViewportState { Pan = new Vector2(100, 50), Zoom = 2.0f };
        var world = new Vector2(75, 125);

        var matrix = viewport.GetTransformMatrix();
        var transformed = Vector2.Transform(world, matrix);
        var expected = viewport.WorldToScreen(world);

        Assert.Equal(expected.X, transformed.X, Tolerance);
        Assert.Equal(expected.Y, transformed.Y, Tolerance);
    }

    [Fact]
    public void GetInverseTransformMatrix_ProducesSameResultAsScreenToWorld()
    {
        var viewport = new ViewportState { Pan = new Vector2(100, 50), Zoom = 2.0f };
        var screen = new Vector2(300, 250);

        var inverseMatrix = viewport.GetInverseTransformMatrix();
        var transformed = Vector2.Transform(screen, inverseMatrix);
        var expected = viewport.ScreenToWorld(screen);

        Assert.Equal(expected.X, transformed.X, Tolerance);
        Assert.Equal(expected.Y, transformed.Y, Tolerance);
    }

    [Fact]
    public void TransformMatrices_AreInverses()
    {
        var viewport = new ViewportState { Pan = new Vector2(-123.4f, 567.8f), Zoom = 3.5f };

        var matrix = viewport.GetTransformMatrix();
        var inverse = viewport.GetInverseTransformMatrix();
        var product = matrix * inverse;

        // Product should be approximately identity
        Assert.Equal(1f, product.M11, Tolerance);
        Assert.Equal(0f, product.M12, Tolerance);
        Assert.Equal(0f, product.M21, Tolerance);
        Assert.Equal(1f, product.M22, Tolerance);
        Assert.Equal(0f, product.M31, 0.001f);
        Assert.Equal(0f, product.M32, 0.001f);
    }

    #endregion

    #region Zoom Constants - Verify documented zoom range

    [Fact]
    public void ZoomRange_Is25PercentTo800Percent()
    {
        Assert.Equal(0.25f, ViewportState.MinZoom);
        Assert.Equal(8.0f, ViewportState.MaxZoom);
    }

    [Fact]
    public void ZoomFactors_AreApproximateInverses()
    {
        // ZoomIn * ZoomOut should be approximately 1.0 for consistent step-by-step zooming
        var product = ViewportState.ZoomInFactor * ViewportState.ZoomOutFactor;
        Assert.Equal(1.0f, product, 0.01f);
    }

    #endregion
}


