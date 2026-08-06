using System;

namespace inkapp.Core.Models;

/// <summary>
/// Represents a bounding rectangle for stroke geometry.
/// Platform-agnostic alternative to Windows.Foundation.Rect.
/// </summary>
public readonly record struct BoundingRect
{
    /// <summary>
    /// The X coordinate of the top-left corner.
    /// </summary>
    public float X { get; init; }

    /// <summary>
    /// The Y coordinate of the top-left corner.
    /// </summary>
    public float Y { get; init; }

    /// <summary>
    /// The width of the rectangle.
    /// </summary>
    public float Width { get; init; }

    /// <summary>
    /// The height of the rectangle.
    /// </summary>
    public float Height { get; init; }

    /// <summary>
    /// Creates a new bounding rectangle.
    /// </summary>
    public BoundingRect(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Gets the left edge (X coordinate).
    /// </summary>
    public float Left => X;

    /// <summary>
    /// Gets the top edge (Y coordinate).
    /// </summary>
    public float Top => Y;

    /// <summary>
    /// Gets the right edge (X + Width).
    /// </summary>
    public float Right => X + Width;

    /// <summary>
    /// Gets the bottom edge (Y + Height).
    /// </summary>
    public float Bottom => Y + Height;

    /// <summary>
    /// Gets whether this rectangle has zero or negative dimensions.
    /// </summary>
    public bool IsEmpty => Width <= 0 || Height <= 0;

    /// <summary>
    /// An empty rectangle at the origin with zero dimensions.
    /// </summary>
    public static BoundingRect Empty => new(0, 0, 0, 0);

    /// <summary>
    /// Creates a rectangle from left, top, right, bottom coordinates.
    /// </summary>
    public static BoundingRect FromLTRB(float left, float top, float right, float bottom) =>
        new(left, top, right - left, bottom - top);

    /// <summary>
    /// Checks if this rectangle intersects with another.
    /// </summary>
    public bool IntersectsWith(BoundingRect other)
    {
        return Left < other.Right && Right > other.Left &&
               Top < other.Bottom && Bottom > other.Top;
    }

    /// <summary>
    /// Checks if a point is inside this rectangle.
    /// </summary>
    public bool Contains(float x, float y)
    {
        return x >= Left && x <= Right && y >= Top && y <= Bottom;
    }

    /// <summary>
    /// Returns a new rectangle expanded by the specified amount on all sides.
    /// </summary>
    public BoundingRect Inflate(float amount) =>
        new(X - amount, Y - amount, Width + amount * 2, Height + amount * 2);
}
