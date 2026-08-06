using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace inkapp.Core.Models;

/// <summary>
/// Represents a complete ink stroke with rendering properties and geometry.
/// </summary>
public sealed class Stroke
{
    private readonly List<StrokePoint> _points;
    private BoundingRect _boundingRect;
    private bool _boundsDirty = true;

    // Pre-allocated capacity to reduce list resizes during drawing
    private const int DefaultPointCapacity = 256;

    /// <summary>
    /// Creates a new stroke with the specified properties.
    /// </summary>
    public Stroke(StrokeColor color, float thickness)
    {
        Id = Guid.NewGuid();
        Color = color;
        Thickness = thickness;
        _points = new List<StrokePoint>(DefaultPointCapacity);
    }

    /// <summary>
    /// Creates a stroke with a specific ID (for deserialization).
    /// </summary>
    public Stroke(Guid id, StrokeColor color, float thickness)
    {
        Id = id;
        Color = color;
        Thickness = thickness;
        _points = new List<StrokePoint>(DefaultPointCapacity);
    }

    /// <summary>
    /// Creates a stroke with a specific ID and points (for deserialization).
    /// </summary>
    public Stroke(Guid id, IEnumerable<StrokePoint> points, StrokeColor color, float thickness)
    {
        Id = id;
        Color = color;
        Thickness = thickness;
        _points = [.. points];
        _boundsDirty = true;
    }

    /// <summary>
    /// Unique identifier for the stroke.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// The color of the stroke.
    /// </summary>
    public StrokeColor Color { get; set; }

    /// <summary>
    /// The thickness of the stroke in world units.
    /// </summary>
    public float Thickness { get; set; }

    /// <summary>
    /// Gets all points in the stroke.
    /// </summary>
    public IReadOnlyList<StrokePoint> Points => _points;

    /// <summary>
    /// Gets a span over the stroke points for high-performance iteration.
    /// Avoids interface dispatch and potential boxing.
    /// </summary>
    public ReadOnlySpan<StrokePoint> PointsSpan => CollectionsMarshal.AsSpan(_points);

    /// <summary>
    /// Gets the axis-aligned bounding rectangle for this stroke.
    /// </summary>
    public BoundingRect BoundingRect
    {
        get
        {
            if (_boundsDirty)
            {
                UpdateBoundingRect();
            }
            return _boundingRect;
        }
    }

    /// <summary>
    /// Adds a point to the stroke.
    /// </summary>
    public void AddPoint(StrokePoint point)
    {
        _points.Add(point);
        _boundsDirty = true;
    }

    /// <summary>
    /// Checks if this stroke intersects with the given rectangle.
    /// </summary>
    public bool Intersects(BoundingRect rect)
    {
        var bounds = BoundingRect;

        // First check bounding rect intersection
        if (!bounds.IntersectsWith(rect))
        {
            return false;
        }

        // For more accurate hit testing, check individual segments
        for (int i = 1; i < _points.Count; i++)
        {
            var p0 = _points[i - 1].Position;
            var p1 = _points[i].Position;

            if (LineIntersectsRect(p0, p1, rect, Thickness))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a point is within the stroke tolerance.
    /// </summary>
    public bool ContainsPoint(Vector2 point, float tolerance)
    {
        var expandedTolerance = tolerance + Thickness / 2f;

        for (int i = 1; i < _points.Count; i++)
        {
            var p0 = _points[i - 1].Position;
            var p1 = _points[i].Position;

            if (DistanceToSegment(point, p0, p1) <= expandedTolerance)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateBoundingRect()
    {
        if (_points.Count == 0)
        {
            _boundingRect = BoundingRect.Empty;
            _boundsDirty = false;
            return;
        }

        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;

        foreach (var point in _points)
        {
            minX = Math.Min(minX, point.Position.X);
            minY = Math.Min(minY, point.Position.Y);
            maxX = Math.Max(maxX, point.Position.X);
            maxY = Math.Max(maxY, point.Position.Y);
        }

        // Expand by stroke thickness
        float halfThickness = Thickness / 2f;
        _boundingRect = new BoundingRect(
            minX - halfThickness,
            minY - halfThickness,
            maxX - minX + Thickness,
            maxY - minY + Thickness);

        _boundsDirty = false;
    }

    private static bool LineIntersectsRect(Vector2 p0, Vector2 p1, BoundingRect rect, float thickness)
    {
        // Expand rect by half thickness for thick line
        float halfT = thickness / 2f;
        var expandedRect = rect.Inflate(halfT);

        // Check if either endpoint is inside
        if (expandedRect.Contains(p0.X, p0.Y) || expandedRect.Contains(p1.X, p1.Y))
        {
            return true;
        }

        // Check line segment against rect edges
        return LineIntersectsRectEdges(p0, p1, expandedRect);
    }

    private static bool LineIntersectsRectEdges(Vector2 p0, Vector2 p1, BoundingRect rect)
    {
        // Cohen-Sutherland-style line clipping test
        var left = rect.Left;
        var right = rect.Right;
        var top = rect.Top;
        var bottom = rect.Bottom;

        // Test against each edge
        return LinesIntersect(p0, p1, new Vector2(left, top), new Vector2(right, top)) ||
               LinesIntersect(p0, p1, new Vector2(right, top), new Vector2(right, bottom)) ||
               LinesIntersect(p0, p1, new Vector2(left, bottom), new Vector2(right, bottom)) ||
               LinesIntersect(p0, p1, new Vector2(left, top), new Vector2(left, bottom));
    }

    private static bool LinesIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
    {
        var d1 = CrossProduct(b2 - b1, a1 - b1);
        var d2 = CrossProduct(b2 - b1, a2 - b1);
        var d3 = CrossProduct(a2 - a1, b1 - a1);
        var d4 = CrossProduct(a2 - a1, b2 - a1);

        if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
            ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
        {
            return true;
        }

        return false;
    }

    private static float CrossProduct(Vector2 a, Vector2 b)
    {
        return a.X * b.Y - a.Y * b.X;
    }

    private static float DistanceToSegment(Vector2 point, Vector2 segStart, Vector2 segEnd)
    {
        var v = segEnd - segStart;
        var w = point - segStart;

        float c1 = Vector2.Dot(w, v);
        if (c1 <= 0)
        {
            return Vector2.Distance(point, segStart);
        }

        float c2 = Vector2.Dot(v, v);
        if (c2 <= c1)
        {
            return Vector2.Distance(point, segEnd);
        }

        float b = c1 / c2;
        var projection = segStart + b * v;
        return Vector2.Distance(point, projection);
    }
}
