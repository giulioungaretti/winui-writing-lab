namespace InkControl.Models;

/// <summary>
/// Configuration for canvas background appearance.
/// </summary>
public sealed record BackgroundSettings
{
    /// <summary>
    /// Default spacing between lines or dots in pixels.
    /// </summary>
    public const double DefaultSpacing = 24.0;

    /// <summary>
    /// Default pattern color as ARGB integer (subtle gray: #20000000).
    /// </summary>
    public static readonly int DefaultPatternColorArgb = unchecked((int)0x20000000);

    /// <summary>
    /// Default background fill color as ARGB integer (white: #FFFFFFFF).
    /// </summary>
    public static readonly int DefaultBackgroundColorArgb = unchecked((int)0xFFFFFFFF);

    /// <summary>
    /// Pattern type (blank, ruled, dotted).
    /// </summary>
    public BackgroundType Type { get; init; } = BackgroundType.Blank;

    /// <summary>
    /// Spacing between lines or dots in pixels.
    /// </summary>
    public double Spacing { get; init; } = DefaultSpacing;

    /// <summary>
    /// Pattern color as ARGB integer.
    /// </summary>
    public int PatternColorArgb { get; init; } = DefaultPatternColorArgb;

    /// <summary>
    /// Background fill color as ARGB integer.
    /// </summary>
    public int BackgroundColorArgb { get; init; } = DefaultBackgroundColorArgb;

    /// <summary>
    /// Default background settings (blank white).
    /// </summary>
    public static BackgroundSettings Default => new();
}
