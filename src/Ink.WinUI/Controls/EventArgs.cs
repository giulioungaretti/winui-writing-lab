using System;
using inkapp.Core.Models;

namespace InkControl.Controls;

/// <summary>
/// Event arguments for viewport changes.
/// </summary>
public sealed class ViewportChangedEventArgs : EventArgs
{
    /// <summary>
    /// Horizontal pan offset.
    /// </summary>
    public float PanX { get; init; }

    /// <summary>
    /// Vertical pan offset.
    /// </summary>
    public float PanY { get; init; }

    /// <summary>
    /// Zoom level.
    /// </summary>
    public float Zoom { get; init; }
}

/// <summary>
/// Event arguments for stroke completion.
/// </summary>
public sealed class StrokeCompletedEventArgs : EventArgs
{
    /// <summary>
    /// The completed stroke.
    /// </summary>
    public required Stroke Stroke { get; init; }
}
