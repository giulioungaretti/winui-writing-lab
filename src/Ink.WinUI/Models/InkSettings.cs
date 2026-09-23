using Windows.UI;

namespace InkControl.Models;

/// <summary>
/// Configuration for ink/pen appearance.
/// </summary>
public sealed record InkSettings
{
    /// <summary>
    /// Default pen thickness in world-space DIPs.
    /// </summary>
    public const double DefaultPenThickness = 2.0;

    /// <summary>
    /// Pen color.
    /// </summary>
    public Color PenColor { get; init; } = Microsoft.UI.Colors.Black;

    /// <summary>
    /// Pen stroke thickness in world-space DIPs. The native engine owns whole-stroke erasing.
    /// </summary>
    public double PenThickness { get; init; } = DefaultPenThickness;

    /// <summary>
    /// Default ink settings (black pen, 2px thickness).
    /// </summary>
    public static InkSettings Default => new();
}
