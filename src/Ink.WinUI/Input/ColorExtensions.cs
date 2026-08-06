using inkapp.Core.Models;
using Windows.UI;

namespace InkControl.Input;

/// <summary>
/// Extension methods for color conversions between Windows.UI.Color and StrokeColor.
/// </summary>
internal static class ColorExtensions
{
    /// <summary>
    /// Converts a Windows.UI.Color to a StrokeColor.
    /// </summary>
    public static StrokeColor ToStrokeColor(this Color color)
    {
        return new StrokeColor(color.A, color.R, color.G, color.B);
    }

    /// <summary>
    /// Converts a StrokeColor to a Windows.UI.Color.
    /// </summary>
    public static Color ToWindowsColor(this StrokeColor color)
    {
        return Color.FromArgb(color.A, color.R, color.G, color.B);
    }
}
