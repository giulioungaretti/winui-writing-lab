using System;
using System.Numerics;

namespace win2dlowlatecny.Rendering;

/// <summary>
/// Manages the transform matrix for infinite canvas pan and zoom operations.
/// </summary>
public sealed class CanvasTransform
{
    private Matrix3x2 _transform;
    private Matrix3x2 _inverseTransform;
    private float _scale;
    private Vector2 _translation;

    /// <summary>
    /// Minimum allowed zoom level.
    /// </summary>
    public const float MinScale = 0.1f;

    /// <summary>
    /// Maximum allowed zoom level.
    /// </summary>
    public const float MaxScale = 10.0f;

    /// <summary>
    /// Current zoom scale (1.0 = 100%).
    /// </summary>
    public float Scale => _scale;

    /// <summary>
    /// Current translation offset in screen coordinates.
    /// </summary>
    public Vector2 Translation => _translation;

    /// <summary>
    /// The transformation matrix from canvas to screen coordinates.
    /// </summary>
    public Matrix3x2 Matrix => _transform;

    /// <summary>
    /// The inverse transformation matrix from screen to canvas coordinates.
    /// </summary>
    public Matrix3x2 InverseMatrix => _inverseTransform;

    /// <summary>
    /// Event raised when the transform changes.
    /// </summary>
    public event EventHandler? TransformChanged;

    public CanvasTransform()
    {
        _scale = 1.0f;
        _translation = Vector2.Zero;
        UpdateTransform();
    }

    /// <summary>
    /// Convert a point from screen coordinates to canvas coordinates.
    /// </summary>
    public Vector2 ScreenToCanvas(Vector2 screenPoint)
    {
        return Vector2.Transform(screenPoint, _inverseTransform);
    }

    /// <summary>
    /// Convert a point from canvas coordinates to screen coordinates.
    /// </summary>
    public Vector2 CanvasToScreen(Vector2 canvasPoint)
    {
        return Vector2.Transform(canvasPoint, _transform);
    }

    /// <summary>
    /// Pan the canvas by a delta in screen coordinates.
    /// </summary>
    public void Pan(Vector2 screenDelta)
    {
        _translation += screenDelta;
        UpdateTransform();
        TransformChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Zoom around a specific screen point.
    /// </summary>
    public void ZoomAt(Vector2 screenCenter, float scaleDelta)
    {
        float newScale = Math.Clamp(_scale * scaleDelta, MinScale, MaxScale);
        if (Math.Abs(newScale - _scale) < 0.0001f)
            return;

        // Get the canvas point under the cursor before zoom
        Vector2 canvasPoint = ScreenToCanvas(screenCenter);

        // Apply the new scale
        _scale = newScale;

        // Calculate new translation to keep the canvas point under the cursor
        // screenCenter = canvasPoint * scale + translation
        // translation = screenCenter - canvasPoint * scale
        _translation = screenCenter - canvasPoint * _scale;

        UpdateTransform();
        TransformChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Set absolute zoom level around a specific screen point.
    /// </summary>
    public void SetZoomAt(Vector2 screenCenter, float absoluteScale)
    {
        float newScale = Math.Clamp(absoluteScale, MinScale, MaxScale);
        if (Math.Abs(newScale - _scale) < 0.0001f)
            return;

        // Get the canvas point under the cursor before zoom
        Vector2 canvasPoint = ScreenToCanvas(screenCenter);

        // Apply the new scale
        _scale = newScale;

        // Calculate new translation to keep the canvas point under the cursor
        _translation = screenCenter - canvasPoint * _scale;

        UpdateTransform();
        TransformChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Reset transform to identity (no pan, no zoom).
    /// </summary>
    public void Reset()
    {
        _scale = 1.0f;
        _translation = Vector2.Zero;
        UpdateTransform();
        TransformChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Get the viewport bounds in canvas coordinates.
    /// </summary>
    public (Vector2 min, Vector2 max) GetViewportBounds(float screenWidth, float screenHeight)
    {
        Vector2 topLeft = ScreenToCanvas(Vector2.Zero);
        Vector2 bottomRight = ScreenToCanvas(new Vector2(screenWidth, screenHeight));
        return (topLeft, bottomRight);
    }

    private void UpdateTransform()
    {
        // Transform is: scale then translate
        // P_screen = P_canvas * scale + translation
        _transform = Matrix3x2.CreateScale(_scale) * Matrix3x2.CreateTranslation(_translation);

        // Calculate inverse for screen to canvas conversion
        Matrix3x2.Invert(_transform, out _inverseTransform);
    }
}
