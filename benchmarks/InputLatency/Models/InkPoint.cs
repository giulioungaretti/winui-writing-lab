using System.Numerics;

namespace win2dlowlatecny.Models;

/// <summary>
/// Represents a single point in an ink stroke with pressure and timing information.
/// </summary>
public readonly struct InkPoint
{
    /// <summary>
    /// Position in canvas coordinates (not screen coordinates).
    /// </summary>
    public Vector2 Position { get; }

    /// <summary>
    /// Pen pressure from 0.0 to 1.0.
    /// </summary>
    public float Pressure { get; }

    /// <summary>
    /// Timestamp when this point was captured (in ticks from Stopwatch).
    /// </summary>
    public long TimestampTicks { get; }

    public InkPoint(Vector2 position, float pressure, long timestampTicks)
    {
        Position = position;
        Pressure = pressure;
        TimestampTicks = timestampTicks;
    }

    public InkPoint(float x, float y, float pressure, long timestampTicks)
        : this(new Vector2(x, y), pressure, timestampTicks)
    {
    }
}
