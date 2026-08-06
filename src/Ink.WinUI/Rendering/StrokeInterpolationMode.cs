namespace InkControl.Rendering;

/// <summary>
/// Specifies the interpolation mode used for rendering ink strokes.
/// </summary>
public enum StrokeInterpolationMode
{
    /// <summary>
    /// Simple line segments between points with round caps.
    /// Fast but may show gaps at pressure transitions.
    /// </summary>
    Linear = 0,

    /// <summary>
    /// Builds a path geometry with variable width outline.
    /// Better quality, fills gaps between segments.
    /// </summary>
    PathGeometry = 1,

    /// <summary>
    /// Uses Catmull-Rom spline interpolation for smooth curves.
    /// Best quality with smooth transitions between points.
    /// </summary>
    CatmullRomSpline = 2
}
