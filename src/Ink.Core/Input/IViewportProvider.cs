using System.Numerics;

namespace inkapp.Core.Input;

/// <summary>
/// Provides viewport transformation capabilities for converting between screen and world coordinates.
/// This abstraction allows the ink input processor to work without direct dependency on ViewportState.
/// </summary>
public interface IViewportProvider
{
    /// <summary>
    /// Converts a screen-space position to world-space position.
    /// </summary>
    /// <param name="screenPosition">Position in screen coordinates (pixels).</param>
    /// <returns>Position in world coordinates.</returns>
    Vector2 ScreenToWorld(Vector2 screenPosition);
    
    /// <summary>
    /// Converts a world-space position to screen-space position.
    /// </summary>
    /// <param name="worldPosition">Position in world coordinates.</param>
    /// <returns>Position in screen coordinates (pixels).</returns>
    Vector2 WorldToScreen(Vector2 worldPosition);
}
