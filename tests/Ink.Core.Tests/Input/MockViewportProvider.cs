using System.Numerics;
using inkapp.Core.Input;

namespace inkapp.Tests.Input;

/// <summary>
/// Mock viewport provider for testing ink input processing.
/// Provides configurable screen-to-world transformation.
/// </summary>
public sealed class MockViewportProvider : IViewportProvider
{
    /// <summary>
    /// Pan offset applied to screen coordinates.
    /// </summary>
    public Vector2 Pan { get; set; } = Vector2.Zero;
    
    /// <summary>
    /// Zoom scale factor.
    /// </summary>
    public float Zoom { get; set; } = 1f;

    /// <summary>
    /// Creates a mock viewport with identity transform (1:1 screen to world).
    /// </summary>
    public MockViewportProvider() { }

    /// <summary>
    /// Creates a mock viewport with specified pan and zoom.
    /// </summary>
    public MockViewportProvider(Vector2 pan, float zoom)
    {
        Pan = pan;
        Zoom = zoom;
    }

    public Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        // Apply inverse zoom and add pan
        return (screenPosition / Zoom) + Pan;
    }

    public Vector2 WorldToScreen(Vector2 worldPosition)
    {
        // Subtract pan and apply zoom
        return (worldPosition - Pan) * Zoom;
    }
}
