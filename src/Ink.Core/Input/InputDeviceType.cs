namespace inkapp.Core.Input;

/// <summary>
/// Types of input devices that can generate ink input.
/// </summary>
public enum InputDeviceType
{
    /// <summary>Unknown device type.</summary>
    Unknown = 0,
    
    /// <summary>Stylus or pen device (pressure-sensitive).</summary>
    Pen,
    
    /// <summary>Touch input (finger or non-stylus touch).</summary>
    Touch,
    
    /// <summary>Mouse input.</summary>
    Mouse
}
