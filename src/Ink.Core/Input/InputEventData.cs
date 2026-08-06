using System.Numerics;

namespace inkapp.Core.Input;

/// <summary>
/// Represents a single input event with all relevant data for ink processing.
/// This is a platform-agnostic representation of pointer input.
/// </summary>
/// <param name="Position">Position in screen coordinates.</param>
/// <param name="Pressure">Pressure value from 0.0 to 1.0. Default pressure is 0.5.</param>
/// <param name="TiltX">Pen tilt angle on X axis in degrees (-90 to 90). 0 for non-pen devices.</param>
/// <param name="TiltY">Pen tilt angle on Y axis in degrees (-90 to 90). 0 for non-pen devices.</param>
/// <param name="DeviceType">The type of input device that generated this event.</param>
/// <param name="PointerId">Unique identifier for the pointer/contact that generated this event.</param>
/// <param name="TimestampTicks">Timestamp of the event in ticks.</param>
public readonly record struct InputEventData(
    Vector2 Position,
    float Pressure,
    float TiltX,
    float TiltY,
    InputDeviceType DeviceType,
    uint PointerId,
    long TimestampTicks)
{
    /// <summary>
    /// Creates input event data with default values for non-pressure devices.
    /// </summary>
    public static InputEventData FromPosition(Vector2 position, InputDeviceType deviceType, uint pointerId)
        => new(position, 0.5f, 0f, 0f, deviceType, pointerId, DateTime.UtcNow.Ticks);
    
    /// <summary>
    /// Creates input event data for pen input with pressure.
    /// </summary>
    public static InputEventData FromPen(Vector2 position, float pressure, uint pointerId)
        => new(position, pressure, 0f, 0f, InputDeviceType.Pen, pointerId, DateTime.UtcNow.Ticks);
    
    /// <summary>
    /// Creates input event data for pen input with pressure and tilt.
    /// </summary>
    public static InputEventData FromPenWithTilt(
        Vector2 position, float pressure, float tiltX, float tiltY, uint pointerId, long? timestampTicks = null)
        => new(position, pressure, tiltX, tiltY, InputDeviceType.Pen, pointerId, timestampTicks ?? DateTime.UtcNow.Ticks);
}
