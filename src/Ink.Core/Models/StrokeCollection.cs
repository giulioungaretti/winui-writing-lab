using System;
using System.Collections.Generic;
using System.Numerics;

namespace inkapp.Core.Models;

/// <summary>
/// A collection of ink strokes with change notification.
/// </summary>
public sealed class StrokeCollection
{
    private readonly List<Stroke> _strokes = [];
    private bool _isDirty;

    /// <summary>
    /// Gets all strokes in the collection.
    /// </summary>
    public IReadOnlyList<Stroke> Strokes => _strokes;

    /// <summary>
    /// Gets whether the collection has been modified since last marked clean.
    /// </summary>
    public bool IsDirty => _isDirty;

    /// <summary>
    /// Raised when the collection changes.
    /// </summary>
    public event EventHandler? StrokesChanged;

    /// <summary>
    /// Adds a stroke to the collection.
    /// </summary>
    public void Add(Stroke stroke)
    {
        ArgumentNullException.ThrowIfNull(stroke);
        _strokes.Add(stroke);
        _isDirty = true;
        StrokesChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Removes a stroke from the collection.
    /// </summary>
    public bool Remove(Stroke stroke)
    {
        if (_strokes.Remove(stroke))
        {
            _isDirty = true;
            StrokesChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Removes all strokes from the collection.
    /// </summary>
    public void Clear()
    {
        if (_strokes.Count > 0)
        {
            _strokes.Clear();
            _isDirty = true;
            StrokesChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Gets strokes that are visible within the given viewport rectangle.
    /// </summary>
    public IEnumerable<Stroke> GetVisibleStrokes(BoundingRect viewportRect)
    {
        foreach (var stroke in _strokes)
        {
            if (stroke.Intersects(viewportRect))
            {
                yield return stroke;
            }
        }
    }

    /// <summary>
    /// Gets strokes that intersect with the given eraser point.
    /// </summary>
    public IEnumerable<Stroke> GetStrokesAt(Vector2 point, float tolerance)
    {
        foreach (var stroke in _strokes)
        {
            if (stroke.ContainsPoint(point, tolerance))
            {
                yield return stroke;
            }
        }
    }

    /// <summary>
    /// Marks the collection as clean (no pending changes).
    /// </summary>
    public void MarkClean()
    {
        _isDirty = false;
    }
}
