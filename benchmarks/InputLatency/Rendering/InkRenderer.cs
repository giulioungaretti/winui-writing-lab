using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI;
using win2dlowlatecny.Diagnostics;
using win2dlowlatecny.Models;
using Windows.UI;

namespace win2dlowlatecny.Rendering;

/// <summary>
/// Renders ink strokes using Win2D for low-latency drawing.
/// </summary>
public sealed class InkRenderer
{
    private readonly LatencyTracker _latencyTracker;
    private CanvasStrokeStyle? _strokeStyle;

    /// <summary>
    /// Background color for the canvas.
    /// </summary>
    public Color BackgroundColor { get; set; } = Microsoft.UI.Colors.White;

    /// <summary>
    /// Grid line color for infinite canvas visualization.
    /// </summary>
    public Color GridColor { get; set; } = Color.FromArgb(30, 128, 128, 128);

    /// <summary>
    /// Grid spacing in canvas units.
    /// </summary>
    public float GridSpacing { get; set; } = 50f;

    /// <summary>
    /// Whether to show the grid.
    /// </summary>
    public bool ShowGrid { get; set; } = true;

    /// <summary>
    /// Whether to show latency overlay.
    /// </summary>
    public bool ShowLatencyOverlay { get; set; } = true;

    public InkRenderer(LatencyTracker latencyTracker)
    {
        _latencyTracker = latencyTracker;
    }

    /// <summary>
    /// Initialize rendering resources.
    /// </summary>
    public void Initialize(CanvasDevice device)
    {
        _strokeStyle = new CanvasStrokeStyle
        {
            StartCap = CanvasCapStyle.Round,
            EndCap = CanvasCapStyle.Round,
            LineJoin = CanvasLineJoin.Round
        };

        Debug.WriteLine("[InkRenderer] Initialized with device");
    }

    /// <summary>
    /// Render the complete frame including grid, strokes, and overlays.
    /// </summary>
    public void Render(
        CanvasDrawingSession ds,
        CanvasTransform transform,
        StrokeCollection completedStrokes,
        InkStroke? activeStroke,
        float screenWidth,
        float screenHeight)
    {
        long renderStartTicks = _latencyTracker.GetTimestamp();

        // Clear background
        ds.Clear(BackgroundColor);

        // Set the transform for canvas-space rendering
        ds.Transform = transform.Matrix;

        // Get viewport bounds for culling
        var (viewMin, viewMax) = transform.GetViewportBounds(screenWidth, screenHeight);

        // Draw grid
        if (ShowGrid)
        {
            RenderGrid(ds, viewMin, viewMax, transform.Scale);
        }

        // Draw completed strokes (with culling)
        var visibleStrokes = completedStrokes.GetVisibleStrokes(viewMin.X, viewMin.Y, viewMax.X, viewMax.Y);
        foreach (var stroke in visibleStrokes)
        {
            RenderStroke(ds, stroke);
        }

        // Draw active stroke (wet ink - highest priority for low latency)
        if (activeStroke != null && activeStroke.PointCount > 0)
        {
            RenderStroke(ds, activeStroke);
        }

        // Reset transform for screen-space overlays
        ds.Transform = Matrix3x2.Identity;

        // Draw latency overlay
        if (ShowLatencyOverlay)
        {
            RenderLatencyOverlay(ds, transform, completedStrokes.Count, screenWidth);
        }

        // Record render latency
        double renderTimeMs = LatencyTracker.TicksToMs(_latencyTracker.GetTimestamp() - renderStartTicks);
        Debug.WriteLine($"[Render] Frame time: {renderTimeMs:F2}ms, Visible strokes: {visibleStrokes.Count}");
    }

    /// <summary>
    /// Render a single ink stroke with pressure-based thickness.
    /// </summary>
    private void RenderStroke(CanvasDrawingSession ds, InkStroke stroke)
    {
        if (stroke.PointCount < 2)
        {
            // Single point - draw a dot
            if (stroke.PointCount == 1)
            {
                var point = stroke.Points[0];
                float radius = stroke.BaseThickness * point.Pressure * 0.5f;
                ds.FillCircle(point.Position, radius, stroke.Color);
            }
            return;
        }

        // Draw stroke as connected line segments with varying thickness
        // For better quality, we could use CanvasPathBuilder for bezier curves
        var points = stroke.Points;

        for (int i = 0; i < points.Count - 1; i++)
        {
            var p1 = points[i];
            var p2 = points[i + 1];

            // Interpolate thickness based on pressure
            float thickness = stroke.BaseThickness * ((p1.Pressure + p2.Pressure) * 0.5f);
            thickness = Math.Max(thickness, 1f); // Minimum 1 pixel

            ds.DrawLine(p1.Position, p2.Position, stroke.Color, thickness, _strokeStyle);
        }

        // Draw end caps for smooth appearance
        var firstPoint = points[0];
        var lastPoint = points[^1];

        float firstRadius = stroke.BaseThickness * firstPoint.Pressure * 0.5f;
        float lastRadius = stroke.BaseThickness * lastPoint.Pressure * 0.5f;

        ds.FillCircle(firstPoint.Position, Math.Max(firstRadius, 0.5f), stroke.Color);
        ds.FillCircle(lastPoint.Position, Math.Max(lastRadius, 0.5f), stroke.Color);
    }

    /// <summary>
    /// Render the infinite canvas grid.
    /// </summary>
    private void RenderGrid(CanvasDrawingSession ds, Vector2 viewMin, Vector2 viewMax, float scale)
    {
        // Adjust grid density based on zoom level
        float adjustedSpacing = GridSpacing;
        while (adjustedSpacing * scale < 20f && adjustedSpacing < 1000f)
        {
            adjustedSpacing *= 2;
        }
        while (adjustedSpacing * scale > 100f && adjustedSpacing > 10f)
        {
            adjustedSpacing /= 2;
        }

        // Calculate grid line thickness based on zoom (thinner when zoomed out)
        float lineThickness = Math.Max(0.5f / scale, 0.1f);

        // Find grid line start positions
        float startX = (float)Math.Floor(viewMin.X / adjustedSpacing) * adjustedSpacing;
        float startY = (float)Math.Floor(viewMin.Y / adjustedSpacing) * adjustedSpacing;

        // Draw vertical lines
        for (float x = startX; x <= viewMax.X; x += adjustedSpacing)
        {
            ds.DrawLine(x, viewMin.Y, x, viewMax.Y, GridColor, lineThickness);
        }

        // Draw horizontal lines
        for (float y = startY; y <= viewMax.Y; y += adjustedSpacing)
        {
            ds.DrawLine(viewMin.X, y, viewMax.X, y, GridColor, lineThickness);
        }

        // Draw origin axes slightly thicker
        Color axisColor = Color.FromArgb(60, 128, 128, 128);
        float axisThickness = lineThickness * 2;

        if (viewMin.X <= 0 && viewMax.X >= 0)
        {
            ds.DrawLine(0, viewMin.Y, 0, viewMax.Y, axisColor, axisThickness);
        }
        if (viewMin.Y <= 0 && viewMax.Y >= 0)
        {
            ds.DrawLine(viewMin.X, 0, viewMax.X, 0, axisColor, axisThickness);
        }
    }

    /// <summary>
    /// Render latency statistics overlay.
    /// </summary>
    private void RenderLatencyOverlay(CanvasDrawingSession ds, CanvasTransform transform, int strokeCount, float screenWidth)
    {
        string stats = _latencyTracker.GetStatisticsString();
        string zoomInfo = $"Zoom: {transform.Scale:P0}";
        string strokeInfo = $"Strokes: {strokeCount}";

        // Semi-transparent background
        ds.FillRectangle(5, 5, 350, 70, Color.FromArgb(180, 0, 0, 0));

        // Draw text
        ds.DrawText(stats, 10, 10, Microsoft.UI.Colors.Lime);
        ds.DrawText(zoomInfo, 10, 30, Microsoft.UI.Colors.White);
        ds.DrawText(strokeInfo, 10, 50, Microsoft.UI.Colors.White);

        // Latency indicator (green if < 20ms, yellow if < 40ms, red otherwise)
        Color indicatorColor;
        if (_latencyTracker.AverageLatencyMs < 20)
            indicatorColor = Microsoft.UI.Colors.Green;
        else if (_latencyTracker.AverageLatencyMs < 40)
            indicatorColor = Microsoft.UI.Colors.Yellow;
        else
            indicatorColor = Microsoft.UI.Colors.Red;

        ds.FillCircle(screenWidth - 20, 20, 10, indicatorColor);
    }

    /// <summary>
    /// Cleanup rendering resources.
    /// </summary>
    public void Dispose()
    {
        _strokeStyle?.Dispose();
        _strokeStyle = null;
    }
}
