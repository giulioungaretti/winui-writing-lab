using System;
using System.Numerics;

namespace inkapp.Core.Models;

/// <summary>
/// Represents the viewport state for a canvas, including pan offset and zoom level.
/// This is used to implement infinite canvas mechanics.
/// </summary>
public record ViewportState
{
    /// <summary>
    /// The pan offset in world coordinates (how much the canvas has been moved).
    /// </summary>
    public Vector2 Pan { get; init; } = Vector2.Zero;

    /// <summary>
    /// The zoom factor (1.0 = 100%, 2.0 = 200%, etc.).
    /// </summary>
    public float Zoom { get; init; } = 1.0f;

    /// <summary>
    /// Minimum allowed zoom factor (25%).
    /// </summary>
    public const float MinZoom = 0.25f;

    /// <summary>
    /// Maximum allowed zoom factor (800%).
    /// </summary>
    public const float MaxZoom = 8.0f;

    /// <summary>
    /// Default zoom step multiplier for zoom in (1.25x).
    /// </summary>
    public const float ZoomInFactor = 1.25f;

    /// <summary>
    /// Default zoom step multiplier for zoom out (0.8x).
    /// </summary>
    public const float ZoomOutFactor = 0.8f;

    /// <summary>
    /// Default viewport state (no pan, 100% zoom).
    /// </summary>
    public static ViewportState Default => new();

    /// <summary>
    /// Creates a new viewport state with the specified pan offset.
    /// </summary>
    public ViewportState WithPan(Vector2 pan) => this with { Pan = pan };

    /// <summary>
    /// Creates a new viewport state with the specified zoom factor.
    /// </summary>
    public ViewportState WithZoom(float zoom) => this with { Zoom = Math.Clamp(zoom, MinZoom, MaxZoom) };

    /// <summary>
    /// Creates a new viewport state by adding a pan delta.
    /// </summary>
    public ViewportState AddPan(Vector2 delta) => this with { Pan = Pan + delta };

    /// <summary>
    /// Converts screen coordinates to world coordinates.
    /// </summary>
    /// <param name="screen">The screen coordinates.</param>
    /// <returns>The world coordinates.</returns>
    public Vector2 ScreenToWorld(Vector2 screen) => (screen - Pan) / Zoom;

    /// <summary>
    /// Converts world coordinates to screen coordinates.
    /// </summary>
    /// <param name="world">The world coordinates.</param>
    /// <returns>The screen coordinates.</returns>
    public Vector2 WorldToScreen(Vector2 world) => (world * Zoom) + Pan;

    /// <summary>
    /// Gets the transformation matrix for rendering (world to screen).
    /// </summary>
    public Matrix3x2 GetTransformMatrix() => 
        Matrix3x2.CreateScale(Zoom) * Matrix3x2.CreateTranslation(Pan);

    /// <summary>
    /// Gets the inverse transformation matrix (screen to world).
    /// </summary>
    public Matrix3x2 GetInverseTransformMatrix()
    {
        Matrix3x2.Invert(GetTransformMatrix(), out var inverse);
        return inverse;
    }

    /// <summary>
    /// Creates a new viewport state with zoom applied around a focal point (screen coordinates).
    /// This keeps the world point under the cursor stationary during zoom.
    /// </summary>
    /// <param name="newZoom">The new zoom factor (will be clamped).</param>
    /// <param name="focalPointScreen">The focal point in screen coordinates (e.g., cursor position).</param>
    /// <returns>A new viewport state with adjusted pan to maintain focal point position.</returns>
    public ViewportState ZoomAroundPoint(float newZoom, Vector2 focalPointScreen)
    {
        newZoom = Math.Clamp(newZoom, MinZoom, MaxZoom);
        
        // If zoom didn't change, return unchanged
        if (Math.Abs(newZoom - Zoom) < 0.0001f)
            return this;

        // Get the world point under the cursor before zoom
        var worldPoint = ScreenToWorld(focalPointScreen);
        
        // Calculate new pan to keep worldPoint at the same screen position after zoom
        // worldPoint * newZoom + newPan = focalPointScreen
        // newPan = focalPointScreen - worldPoint * newZoom
        var newPan = focalPointScreen - (worldPoint * newZoom);
        
        return new ViewportState
        {
            Pan = newPan,
            Zoom = newZoom
        };
    }
}
