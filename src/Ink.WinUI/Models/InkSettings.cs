using Windows.UI;

namespace InkControl.Models;

/// <summary>
/// Configuration for ink/pen appearance.
/// </summary>
public sealed record InkSettings
{
    /// <summary>
    /// Default pen thickness in pixels.
    /// </summary>
    public const double DefaultPenThickness = 2.0;

    /// <summary>
    /// Default eraser radius in pixels.
    /// </summary>
    public const double DefaultEraserRadius = 10.0;

    /// <summary>
    /// Pen color.
    /// </summary>
    public Color PenColor { get; init; } = Microsoft.UI.Colors.Black;

    /// <summary>
    /// Pen stroke thickness in pixels.
    /// </summary>
    public double PenThickness { get; init; } = DefaultPenThickness;

    /// <summary>
    /// Eraser radius in pixels.
    /// </summary>
    public double EraserRadius { get; init; } = DefaultEraserRadius;

    /// <summary>
    /// Default ink settings (black pen, 2px thickness).
    /// </summary>
    public static InkSettings Default => new();
}
