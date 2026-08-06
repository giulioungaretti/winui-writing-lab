using inkapp.Core.Models;
using Xunit;

namespace inkapp.Tests.Models;

/// <summary>
/// Tests for StrokeColor, a platform-agnostic ARGB color type.
/// 
/// StrokeColor provides:
/// - Constructor and factory methods for color creation
/// - Predefined colors for common use cases
/// - Value equality semantics
/// </summary>
public class StrokeColorTests
{
    #region Construction

    [Fact]
    public void Constructor_SetsArgbComponents()
    {
        var color = new StrokeColor(128, 255, 100, 50);

        Assert.Equal(128, color.A);
        Assert.Equal(255, color.R);
        Assert.Equal(100, color.G);
        Assert.Equal(50, color.B);
    }

    [Fact]
    public void FromArgb_CreatesColorWithSpecifiedComponents()
    {
        var color = StrokeColor.FromArgb(200, 150, 100, 50);

        Assert.Equal(200, color.A);
        Assert.Equal(150, color.R);
        Assert.Equal(100, color.G);
        Assert.Equal(50, color.B);
    }

    [Fact]
    public void FromRgb_CreatesOpaqueColor()
    {
        var color = StrokeColor.FromRgb(150, 100, 50);

        Assert.Equal(255, color.A);
        Assert.Equal(150, color.R);
        Assert.Equal(100, color.G);
        Assert.Equal(50, color.B);
    }

    #endregion

    #region Predefined Colors

    [Theory]
    [InlineData(nameof(StrokeColor.Black), 255, 0, 0, 0)]
    [InlineData(nameof(StrokeColor.White), 255, 255, 255, 255)]
    [InlineData(nameof(StrokeColor.Red), 255, 255, 0, 0)]
    [InlineData(nameof(StrokeColor.Green), 255, 0, 128, 0)]  // HTML green
    [InlineData(nameof(StrokeColor.Blue), 255, 0, 0, 255)]
    [InlineData(nameof(StrokeColor.Transparent), 0, 0, 0, 0)]
    public void PredefinedColors_HaveCorrectValues(string colorName, byte a, byte r, byte g, byte b)
    {
        var color = colorName switch
        {
            nameof(StrokeColor.Black) => StrokeColor.Black,
            nameof(StrokeColor.White) => StrokeColor.White,
            nameof(StrokeColor.Red) => StrokeColor.Red,
            nameof(StrokeColor.Green) => StrokeColor.Green,
            nameof(StrokeColor.Blue) => StrokeColor.Blue,
            nameof(StrokeColor.Transparent) => StrokeColor.Transparent,
            _ => throw new ArgumentException($"Unknown color: {colorName}")
        };

        Assert.Equal(a, color.A);
        Assert.Equal(r, color.R);
        Assert.Equal(g, color.G);
        Assert.Equal(b, color.B);
    }

    #endregion

    #region Equality

    [Fact]
    public void Equality_TrueForSameArgbValues()
    {
        var color1 = StrokeColor.FromArgb(200, 150, 100, 50);
        var color2 = StrokeColor.FromArgb(200, 150, 100, 50);

        Assert.Equal(color1, color2);
    }

    [Theory]
    [InlineData(199, 150, 100, 50)]  // Different alpha
    [InlineData(200, 151, 100, 50)]  // Different red
    [InlineData(200, 150, 101, 50)]  // Different green
    [InlineData(200, 150, 100, 51)]  // Different blue
    public void Equality_FalseWhenAnyComponentDiffers(byte a, byte r, byte g, byte b)
    {
        var color1 = StrokeColor.FromArgb(200, 150, 100, 50);
        var color2 = StrokeColor.FromArgb(a, r, g, b);

        Assert.NotEqual(color1, color2);
    }

    #endregion
}
