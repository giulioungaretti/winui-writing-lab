using InkControl.Models;
using Vortice.Mathematics;

namespace InkControl.Rendering;

/// <summary>
/// Background settings for D2D rendering.
/// </summary>
/// <param name="Type">Background pattern type.</param>
/// <param name="Spacing">Spacing between pattern elements.</param>
/// <param name="PatternColor">Color for the pattern elements.</param>
/// <param name="BackgroundColor">Background fill color.</param>
public readonly record struct D2DBackgroundSettings(
    BackgroundType Type,
    float Spacing,
    Color4 PatternColor,
    Color4 BackgroundColor)
{
    /// <summary>
    /// Default pattern color as packed ARGB integer (subtle gray with alpha).
    /// </summary>
    public const int DefaultPatternColorArgb = unchecked((int)0x20000000);
}
