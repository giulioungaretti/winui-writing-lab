using System.Collections.Generic;

namespace win2dlowlatecny.Models;

/// <summary>
/// Thread-safe collection of completed ink strokes.
/// </summary>
public sealed class StrokeCollection
{
    private readonly List<InkStroke> _strokes;
    private readonly object _lock = new();
    private int _nextStrokeId;

    /// <summary>
    /// Number of strokes in the collection.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _strokes.Count;
            }
        }
    }

    public StrokeCollection()
    {
        _strokes = new List<InkStroke>(1024); // Pre-allocate for performance
    }

    /// <summary>
    /// Generate a unique ID for a new stroke.
    /// </summary>
    public int GetNextStrokeId()
    {
        lock (_lock)
        {
            return _nextStrokeId++;
        }
    }

    /// <summary>
    /// Add a completed stroke to the collection.
    /// </summary>
    public void Add(InkStroke stroke)
    {
        lock (_lock)
        {
            _strokes.Add(stroke);
        }
    }

    /// <summary>
    /// Get strokes that intersect with the given viewport.
    /// Returns a copy of the list to avoid threading issues.
    /// </summary>
    public List<InkStroke> GetVisibleStrokes(float viewMinX, float viewMinY, float viewMaxX, float viewMaxY)
    {
        var visible = new List<InkStroke>();
        lock (_lock)
        {
            foreach (var stroke in _strokes)
            {
                if (stroke.IntersectsViewport(viewMinX, viewMinY, viewMaxX, viewMaxY))
                {
                    visible.Add(stroke);
                }
            }
        }
        return visible;
    }

    /// <summary>
    /// Get all strokes (for scenarios where culling is not needed).
    /// Returns a copy to avoid threading issues.
    /// </summary>
    public List<InkStroke> GetAllStrokes()
    {
        lock (_lock)
        {
            return new List<InkStroke>(_strokes);
        }
    }

    /// <summary>
    /// Clear all strokes from the collection.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _strokes.Clear();
        }
    }

    /// <summary>
    /// Remove the last stroke (undo operation).
    /// </summary>
    public bool RemoveLast()
    {
        lock (_lock)
        {
            if (_strokes.Count > 0)
            {
                _strokes.RemoveAt(_strokes.Count - 1);
                return true;
            }
            return false;
        }
    }
}
