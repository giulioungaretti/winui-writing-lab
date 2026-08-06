using System.Collections.Generic;
using System.Numerics;
using Windows.UI;

namespace win2dlowlatecny.Models;

/// <summary>
/// Represents a complete or in-progress ink stroke.
/// </summary>
public sealed class InkStroke
{
    private readonly List<InkPoint> _points;

    /// <summary>
    /// Unique identifier for this stroke.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Color of the stroke.
    /// </summary>
    public Color Color { get; set; }

    /// <summary>
    /// Base thickness of the stroke (will be modified by pressure).
    /// </summary>
    public float BaseThickness { get; set; }

    /// <summary>
    /// Minimum bounding box X coordinate in canvas space.
    /// </summary>
    public float BoundsMinX { get; private set; }

    /// <summary>
    /// Maximum bounding box X coordinate in canvas space.
    /// </summary>
    public float BoundsMaxX { get; private set; }

    /// <summary>
    /// Minimum bounding box Y coordinate in canvas space.
    /// </summary>
    public float BoundsMinY { get; private set; }

    /// <summary>
    /// Maximum bounding box Y coordinate in canvas space.
    /// </summary>
    public float BoundsMaxY { get; private set; }

    /// <summary>
    /// Whether this stroke is complete (pen lifted).
    /// </summary>
    public bool IsComplete { get; private set; }

    /// <summary>
    /// Read-only access to the points in this stroke.
    /// </summary>
    public IReadOnlyList<InkPoint> Points => _points;

    /// <summary>
    /// Number of points in this stroke.
    /// </summary>
    public int PointCount => _points.Count;

    public InkStroke(int id, Color color, float baseThickness)
    {
        Id = id;
        Color = color;
        BaseThickness = baseThickness;
        _points = new List<InkPoint>(256); // Pre-allocate for performance
        BoundsMinX = float.MaxValue;
        BoundsMaxX = float.MinValue;
        BoundsMinY = float.MaxValue;
        BoundsMaxY = float.MinValue;
    }

    /// <summary>
    /// Add a point to this stroke and update bounds.
    /// </summary>
    public void AddPoint(InkPoint point)
    {
        _points.Add(point);
        UpdateBounds(point.Position);
    }

    /// <summary>
    /// Add multiple points to this stroke efficiently.
    /// </summary>
    public void AddPoints(IEnumerable<InkPoint> points)
    {
        foreach (var point in points)
        {
            _points.Add(point);
            UpdateBounds(point.Position);
        }
    }

    /// <summary>
    /// Mark this stroke as complete.
    /// </summary>
    public void Complete()
    {
        IsComplete = true;
        // Trim excess capacity for memory efficiency
        _points.TrimExcess();
    }

    /// <summary>
    /// Check if this stroke intersects with a viewport rectangle.
    /// </summary>
    public bool IntersectsViewport(float viewMinX, float viewMinY, float viewMaxX, float viewMaxY)
    {
        // Expand bounds by max possible stroke width for accurate culling
        float expansion = BaseThickness * 2;
        return BoundsMaxX + expansion >= viewMinX &&
               BoundsMinX - expansion <= viewMaxX &&
               BoundsMaxY + expansion >= viewMinY &&
               BoundsMinY - expansion <= viewMaxY;
    }

    private void UpdateBounds(Vector2 position)
    {
        if (position.X < BoundsMinX) BoundsMinX = position.X;
        if (position.X > BoundsMaxX) BoundsMaxX = position.X;
        if (position.Y < BoundsMinY) BoundsMinY = position.Y;
        if (position.Y > BoundsMaxY) BoundsMaxY = position.Y;
    }
}
