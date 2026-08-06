using System.Numerics;
using inkapp.Core.Models;
using Xunit;

namespace inkapp.Tests.Models;

/// <summary>
/// Tests for StrokePoint, which captures pen input data.
/// 
/// StrokePoint stores per-point data for ink rendering:
/// - Position: world coordinates of the point
/// - Pressure: pen pressure (0.0-1.0) for variable stroke width
/// - TiltX/TiltY: pen tilt angles for advanced rendering effects
/// - TimestampTicks: for velocity-based smoothing
/// </summary>
public class StrokePointTests
{
    #region Construction

    [Fact]
    public void FullConstructor_SetsAllProperties()
    {
        var point = new StrokePoint(
            new Vector2(100, 200),
            0.75f,
            15f,
            -10f,
            12345678L);

        Assert.Equal(new Vector2(100, 200), point.Position);
        Assert.Equal(0.75f, point.Pressure);
        Assert.Equal(15f, point.TiltX);
        Assert.Equal(-10f, point.TiltY);
        Assert.Equal(12345678L, point.TimestampTicks);
    }

    [Fact]
    public void SimplifiedConstructor_DefaultsTiltToZero()
    {
        // For devices that don't report tilt
        var point = new StrokePoint(
            new Vector2(50, 75),
            0.5f,
            98765432L);

        Assert.Equal(new Vector2(50, 75), point.Position);
        Assert.Equal(0.5f, point.Pressure);
        Assert.Equal(0f, point.TiltX);
        Assert.Equal(0f, point.TiltY);
        Assert.Equal(98765432L, point.TimestampTicks);
    }

    #endregion

    #region Value Range Handling

    [Theory]
    [InlineData(0f)]    // Minimum pressure (hovering)
    [InlineData(0.5f)]  // Normal pressure
    [InlineData(1f)]    // Maximum pressure
    public void Pressure_AcceptsFullRange(float pressure)
    {
        var point = new StrokePoint(Vector2.Zero, pressure, 0, 0, 0);
        Assert.Equal(pressure, point.Pressure);
    }

    [Theory]
    [InlineData(90f, -90f)]   // Maximum tilt
    [InlineData(-90f, 90f)]  // Opposite maximum
    [InlineData(0f, 0f)]      // No tilt (perpendicular)
    public void Tilt_AcceptsFullRange(float tiltX, float tiltY)
    {
        var point = new StrokePoint(Vector2.Zero, 0.5f, tiltX, tiltY, 0);
        Assert.Equal(tiltX, point.TiltX);
        Assert.Equal(tiltY, point.TiltY);
    }

    [Fact]
    public void Position_AcceptsNegativeCoordinates()
    {
        var point = new StrokePoint(new Vector2(-100, -200), 0.5f, 0, 0, 0);
        Assert.Equal(new Vector2(-100, -200), point.Position);
    }

    #endregion

    #region Equality

    [Fact]
    public void Equality_TrueForIdenticalValues()
    {
        var point1 = new StrokePoint(new Vector2(10, 20), 0.5f, 5f, -5f, 1000);
        var point2 = new StrokePoint(new Vector2(10, 20), 0.5f, 5f, -5f, 1000);

        Assert.Equal(point1, point2);
    }

    [Fact]
    public void Equality_FalseForDifferentPosition()
    {
        var point1 = new StrokePoint(new Vector2(10, 20), 0.5f, 5f, -5f, 1000);
        var point2 = new StrokePoint(new Vector2(10, 21), 0.5f, 5f, -5f, 1000);

        Assert.NotEqual(point1, point2);
    }

    [Fact]
    public void Equality_FalseForDifferentPressure()
    {
        var point1 = new StrokePoint(new Vector2(10, 20), 0.5f, 5f, -5f, 1000);
        var point2 = new StrokePoint(new Vector2(10, 20), 0.6f, 5f, -5f, 1000);

        Assert.NotEqual(point1, point2);
    }

    [Fact]
    public void Equality_FalseForDifferentTimestamp()
    {
        var point1 = new StrokePoint(new Vector2(10, 20), 0.5f, 5f, -5f, 1000);
        var point2 = new StrokePoint(new Vector2(10, 20), 0.5f, 5f, -5f, 1001);

        Assert.NotEqual(point1, point2);
    }

    #endregion
}
