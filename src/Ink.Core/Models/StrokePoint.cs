using System.Numerics;

namespace inkapp.Core.Models;

/// <summary>
/// Represents a single point in an ink stroke with pressure, tilt, and timing data.
/// </summary>
/// <param name="Position">World coordinates of the point.</param>
/// <param name="Pressure">Pen pressure from 0.0 to 1.0.</param>
/// <param name="TiltX">Pen tilt on X axis from -90 to 90 degrees (0 = perpendicular).</param>
/// <param name="TiltY">Pen tilt on Y axis from -90 to 90 degrees (0 = perpendicular).</param>
/// <param name="TimestampTicks">High-resolution timestamp for velocity calculations.</param>
public readonly record struct StrokePoint(
    Vector2 Position,
    float Pressure,
    float TiltX,
    float TiltY,
    long TimestampTicks)
{
    /// <summary>
    /// Creates a StrokePoint without tilt data (for backward compatibility).
    /// </summary>
    public StrokePoint(Vector2 position, float pressure, long timestampTicks)
        : this(position, pressure, 0f, 0f, timestampTicks)
    {
    }
}
