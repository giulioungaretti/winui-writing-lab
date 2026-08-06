using System;

namespace inkapp.Core.Models;

/// <summary>
/// Represents an ARGB color for ink strokes.
/// Platform-agnostic alternative to Windows.UI.Color.
/// </summary>
public readonly record struct StrokeColor(byte A, byte R, byte G, byte B)
{
    /// <summary>
    /// Creates a color from ARGB byte values.
    /// </summary>
    public static StrokeColor FromArgb(byte a, byte r, byte g, byte b) => new(a, r, g, b);

    /// <summary>
    /// Creates an opaque color from RGB byte values.
    /// </summary>
    public static StrokeColor FromRgb(byte r, byte g, byte b) => new(255, r, g, b);

    /// <summary>
    /// Black color.
    /// </summary>
    public static StrokeColor Black => new(255, 0, 0, 0);

    /// <summary>
    /// White color.
    /// </summary>
    public static StrokeColor White => new(255, 255, 255, 255);

    /// <summary>
    /// Red color.
    /// </summary>
    public static StrokeColor Red => new(255, 255, 0, 0);

    /// <summary>
    /// Green color.
    /// </summary>
    public static StrokeColor Green => new(255, 0, 128, 0);

    /// <summary>
    /// Blue color.
    /// </summary>
    public static StrokeColor Blue => new(255, 0, 0, 255);

    /// <summary>
    /// Transparent color.
    /// </summary>
    public static StrokeColor Transparent => new(0, 0, 0, 0);
}
