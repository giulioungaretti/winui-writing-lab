using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using InkControl.Models;
using inkapp.Core.Models;
using Vortice.Direct2D1;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace InkControl.Rendering;

/// <summary>
/// Renders ink strokes using Direct2D for high-performance, low-latency drawing.
/// Uses Vortice.Windows for DirectX interop.
/// </summary>
internal sealed class D2DInkRenderer : IDisposable
{
    private readonly SwapChainManager _swapChain;
    private readonly DeviceManager _deviceManager;
    private readonly Dictionary<uint, ID2D1SolidColorBrush> _brushCache = [];
    private bool _isDisposed;

    // Cached bitmap for static content (background + completed strokes)
    private ID2D1Bitmap1? _staticContentBitmap;
    private bool _staticContentDirty = true;
    private int _cachedStrokeCount;
    private uint _cachedWidth;
    private uint _cachedHeight;

    // Track the last rendered point index for incremental stroke rendering
    private int _lastRenderedPointIndex;

    /// <summary>
    /// Gets or sets the stroke interpolation mode.
    /// </summary>
    public StrokeInterpolationMode InterpolationMode { get; set; } = StrokeInterpolationMode.Linear;

    /// <summary>
    /// Gets or sets whether cached bitmap rendering is enabled.
    /// When disabled, full canvas is redrawn every frame.
    /// </summary>
    public bool IsCachedRenderingEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether incremental stroke rendering is enabled.
    /// When enabled, only new segments of the current stroke are rendered each frame.
    /// </summary>
    public bool IsIncrementalStrokeRenderingEnabled { get; set; } = true;

    public D2DInkRenderer(SwapChainManager swapChain, DeviceManager deviceManager)
    {
        ArgumentNullException.ThrowIfNull(swapChain);
        ArgumentNullException.ThrowIfNull(deviceManager);

        _swapChain = swapChain;
        _deviceManager = deviceManager;
    }

    /// <summary>
    /// Resets the incremental stroke rendering state.
    /// Call this when a new stroke begins.
    /// </summary>
    public void ResetIncrementalState()
    {
        _lastRenderedPointIndex = 0;
    }

    /// <summary>
    /// Marks the static content (background + completed strokes) as dirty,
    /// requiring a full re-render on the next frame.
    /// </summary>
    public void InvalidateStaticContent()
    {
        _staticContentDirty = true;
    }

    /// <summary>
    /// Renders all strokes with the current viewport transform and background.
    /// </summary>
    /// <param name="strokes">Collection of completed strokes to render.</param>
    /// <param name="currentStroke">The stroke currently being drawn, if any.</param>
    /// <param name="viewport">Current viewport state for transforms.</param>
    /// <param name="background">Background rendering settings.</param>
    /// <param name="isActivelyDrawing">If true, uses optimized rendering path for lower latency.</param>
    public void Render(
        StrokeCollection strokes,
        Stroke? currentStroke,
        ViewportState viewport,
        D2DBackgroundSettings background,
        bool isActivelyDrawing = false)
    {
        ThrowIfDisposed();

        var context = _swapChain.D2DContext;
        if (context is null) return;

        // If cached rendering is disabled, always do full render
        if (!IsCachedRenderingEnabled)
        {
            RenderFullNonCached(context, strokes, currentStroke, viewport, background);
            _swapChain.Present(isActivelyDrawing ? 0u : 1u);
            return;
        }

        // Check if static content needs to be re-rendered
        bool needsStaticUpdate = _staticContentDirty
            || strokes.Strokes.Count != _cachedStrokeCount
            || _swapChain.Width != _cachedWidth
            || _swapChain.Height != _cachedHeight
            || _staticContentBitmap is null;

        // During active drawing with valid cache, use optimized path
        if (isActivelyDrawing && !needsStaticUpdate && _staticContentBitmap is not null)
        {
            RenderWithCachedStatic(context, currentStroke, viewport);
        }
        else
        {
            // Full render - update cache
            RenderFull(context, strokes, currentStroke, viewport, background);
            _staticContentDirty = false;
            _cachedStrokeCount = strokes.Strokes.Count;
            _cachedWidth = _swapChain.Width;
            _cachedHeight = _swapChain.Height;
        }

        // Present immediately during drawing, vsync otherwise
        _swapChain.Present(isActivelyDrawing ? 0u : 1u);
    }

    /// <summary>
    /// Full render without caching - renders everything every frame.
    /// Used when cached rendering is disabled.
    /// </summary>
    private void RenderFullNonCached(
        ID2D1DeviceContext context,
        StrokeCollection strokes,
        Stroke? currentStroke,
        ViewportState viewport,
        D2DBackgroundSettings background)
    {
        _swapChain.BeginDraw();

        try
        {
            // Clear with background color
            context.Clear(background.BackgroundColor);

            // Apply viewport transform for background pattern
            context.Transform = viewport.GetTransformMatrix();

            // Render background pattern (in world coordinates)
            RenderBackground(context, background, viewport);

            // Render completed strokes
            foreach (var stroke in strokes.Strokes)
            {
                RenderStroke(context, stroke);
            }

            // Render current stroke being drawn
            if (currentStroke is not null)
            {
                RenderStroke(context, currentStroke);
            }

            // Reset transform
            context.Transform = Matrix3x2.Identity;
        }
        finally
        {
            _swapChain.EndDraw();
        }
    }

    /// <summary>
    /// Optimized render path: draws cached static content + current stroke only.
    /// </summary>
    private void RenderWithCachedStatic(ID2D1DeviceContext context, Stroke? currentStroke, ViewportState viewport)
    {
        _swapChain.BeginDraw();

        try
        {
            // Draw the cached static content (background + completed strokes)
            context.Transform = Matrix3x2.Identity;

            // Draw the cached bitmap at origin
            context.DrawBitmap(_staticContentBitmap!);

            // Render the current stroke being drawn
            if (currentStroke is not null)
            {
                context.Transform = viewport.GetTransformMatrix();

                if (IsIncrementalStrokeRenderingEnabled)
                {
                    // Incremental: only render new segments since last frame
                    RenderStrokeIncremental(context, currentStroke);
                }
                else
                {
                    // Full: render entire current stroke
                    RenderStroke(context, currentStroke);
                }
            }

            context.Transform = Matrix3x2.Identity;
        }
        finally
        {
            _swapChain.EndDraw();
        }
    }

    /// <summary>
    /// Renders only new segments of the current stroke since the last frame.
    /// </summary>
    private void RenderStrokeIncremental(ID2D1DeviceContext context, Stroke stroke)
    {
        if (stroke.Points.Count < 2)
        {
            if (stroke.Points.Count == 1 && _lastRenderedPointIndex == 0)
            {
                RenderDot(context, stroke.Points[0], stroke);
                _lastRenderedPointIndex = 1;
            }
            return;
        }

        var brush = GetOrCreateBrush(context, stroke.Color);
        var strokeStyle = _deviceManager.RoundStrokeStyle;

        // Start from where we left off, but ensure we have at least one previous point for the segment
        int startIndex = Math.Max(1, _lastRenderedPointIndex);

        // Render only new segments
        for (int i = startIndex; i < stroke.Points.Count; i++)
        {
            var p0 = stroke.Points[i - 1];
            var p1 = stroke.Points[i];

            float thickness = CalculateThickness(p0, p1, stroke.Thickness);

            context.DrawLine(
                new Vector2(p0.Position.X, p0.Position.Y),
                new Vector2(p1.Position.X, p1.Position.Y),
                brush,
                thickness,
                strokeStyle);
        }

        // Update the last rendered point index
        _lastRenderedPointIndex = stroke.Points.Count;
    }

    /// <summary>
    /// Full render path: renders everything and updates the static content cache.
    /// </summary>
    private void RenderFull(
        ID2D1DeviceContext context,
        StrokeCollection strokes,
        Stroke? currentStroke,
        ViewportState viewport,
        D2DBackgroundSettings background)
    {
        _swapChain.BeginDraw();

        try
        {
            // Clear with background color
            context.Clear(background.BackgroundColor);

            // Apply viewport transform for background pattern
            context.Transform = viewport.GetTransformMatrix();

            // Render background pattern (in world coordinates)
            RenderBackground(context, background, viewport);

            // Render completed strokes
            foreach (var stroke in strokes.Strokes)
            {
                RenderStroke(context, stroke);
            }

            // Reset transform before capturing static content
            context.Transform = Matrix3x2.Identity;
        }
        finally
        {
            _swapChain.EndDraw();
        }

        // Cache the static content BEFORE rendering current stroke
        CaptureStaticContent(context);

        // Reset incremental state since we're doing a full render
        _lastRenderedPointIndex = 0;

        // Now render the current stroke on top (if any)
        if (currentStroke is not null)
        {
            _swapChain.BeginDraw();
            try
            {
                context.Transform = viewport.GetTransformMatrix();
                RenderStroke(context, currentStroke);

                // Update last rendered point index for next incremental frame
                _lastRenderedPointIndex = currentStroke.Points.Count;
                context.Transform = Matrix3x2.Identity;
            }
            finally
            {
                _swapChain.EndDraw();
            }
        }
    }

    /// <summary>
    /// Captures the current back buffer to a bitmap for caching.
    /// </summary>
    private void CaptureStaticContent(ID2D1DeviceContext context)
    {
        // Dispose old bitmap if size changed
        if (_staticContentBitmap is not null &&
            (_cachedWidth != _swapChain.Width || _cachedHeight != _swapChain.Height))
        {
            _staticContentBitmap.Dispose();
            _staticContentBitmap = null;
        }

        // Create bitmap if needed
        if (_staticContentBitmap is null)
        {
            var bitmapProps = new BitmapProperties1
            {
                PixelFormat = new Vortice.DCommon.PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied),
                DpiX = 96f,
                DpiY = 96f,
                BitmapOptions = BitmapOptions.None // Can be drawn
            };

            var size = new SizeI((int)_swapChain.Width, (int)_swapChain.Height);
            _staticContentBitmap = context.CreateBitmap(size, IntPtr.Zero, 0, bitmapProps);
        }

        // Copy from the render target
        if (context.Target is ID2D1Bitmap1 targetBitmap)
        {
            var destPoint = new System.Drawing.Point(0, 0);
            var srcRect = new System.Drawing.Rectangle(0, 0, (int)_swapChain.Width, (int)_swapChain.Height);
            _staticContentBitmap.CopyFromBitmap(destPoint, targetBitmap, srcRect);
        }
    }

    private void RenderBackground(ID2D1DeviceContext context, D2DBackgroundSettings settings, ViewportState viewport)
    {
        if (settings.Type == BackgroundType.Blank)
            return;

        var brush = GetOrCreateBrush(context, settings.PatternColor);

        // Calculate visible area in world coordinates
        var viewportSize = new Vector2(_swapChain.Width, _swapChain.Height);
        var worldTopLeft = viewport.ScreenToWorld(Vector2.Zero);
        var worldBottomRight = viewport.ScreenToWorld(viewportSize);

        // Add margin to ensure patterns extend beyond visible area
        float margin = settings.Spacing * 2;
        float startX = MathF.Floor((worldTopLeft.X - margin) / settings.Spacing) * settings.Spacing;
        float startY = MathF.Floor((worldTopLeft.Y - margin) / settings.Spacing) * settings.Spacing;
        float endX = worldBottomRight.X + margin;
        float endY = worldBottomRight.Y + margin;

        switch (settings.Type)
        {
            case BackgroundType.Ruled:
                RenderRuledBackground(context, brush, startX, startY, endX, endY, settings.Spacing);
                break;
            case BackgroundType.Dotted:
                RenderDottedBackground(context, brush, startX, startY, endX, endY, settings.Spacing, viewport.Zoom);
                break;
        }
    }

    private void RenderRuledBackground(
        ID2D1DeviceContext context,
        ID2D1SolidColorBrush brush,
        float startX, float startY, float endX, float endY,
        float spacing)
    {
        // Draw horizontal ruled lines
        for (float y = startY; y <= endY; y += spacing)
        {
            context.DrawLine(
                new Vector2(startX, y),
                new Vector2(endX, y),
                brush,
                1f / context.Transform.M11); // Scale-invariant line width
        }
    }

    private void RenderDottedBackground(
        ID2D1DeviceContext context,
        ID2D1SolidColorBrush brush,
        float startX, float startY, float endX, float endY,
        float spacing, float zoom)
    {
        // Dot radius that stays visually consistent
        float dotRadius = 1.5f / zoom;

        // Limit dot count for performance
        int maxDotsPerAxis = 200;
        float actualSpacingX = Math.Max(spacing, (endX - startX) / maxDotsPerAxis);
        float actualSpacingY = Math.Max(spacing, (endY - startY) / maxDotsPerAxis);

        for (float y = startY; y <= endY; y += actualSpacingY)
        {
            for (float x = startX; x <= endX; x += actualSpacingX)
            {
                var ellipse = new Ellipse(new Vector2(x, y), dotRadius, dotRadius);
                context.FillEllipse(ellipse, brush);
            }
        }
    }

    private void RenderStroke(ID2D1DeviceContext context, Stroke stroke)
    {
        if (stroke.Points.Count < 2)
        {
            if (stroke.Points.Count == 1)
            {
                RenderDot(context, stroke.Points[0], stroke);
            }
            return;
        }

        switch (InterpolationMode)
        {
            case StrokeInterpolationMode.PathGeometry:
                RenderStrokeWithPathGeometry(context, stroke);
                break;
            case StrokeInterpolationMode.CatmullRomSpline:
                RenderStrokeWithCatmullRom(context, stroke);
                break;
            case StrokeInterpolationMode.Linear:
            default:
                RenderStrokeLinear(context, stroke);
                break;
        }
    }

    /// <summary>
    /// Renders stroke using simple line segments (original implementation).
    /// </summary>
    private void RenderStrokeLinear(ID2D1DeviceContext context, Stroke stroke)
    {
        var brush = GetOrCreateBrush(context, stroke.Color);
        var strokeStyle = _deviceManager.RoundStrokeStyle;
        var points = stroke.PointsSpan;

        // Draw connected line segments with pressure and tilt-based thickness
        for (int i = 1; i < points.Length; i++)
        {
            ref readonly var p0 = ref points[i - 1];
            ref readonly var p1 = ref points[i];

            float avgPressure = (p0.Pressure + p1.Pressure) * 0.5f;

            // Calculate tilt factor - tilted pen creates wider strokes
            float avgTiltX = (p0.TiltX + p1.TiltX) * 0.5f;
            float avgTiltY = (p0.TiltY + p1.TiltY) * 0.5f;
            float tiltMagnitude = MathF.Sqrt(avgTiltX * avgTiltX + avgTiltY * avgTiltY);
            float tiltFactor = 1.0f + (MathF.Min(tiltMagnitude, 45f) * (1f / 45f)) * 0.5f;

            float thickness = stroke.Thickness * MathF.Max(0.3f, avgPressure) * tiltFactor;

            context.DrawLine(
                p0.Position,
                p1.Position,
                brush,
                thickness,
                strokeStyle);
        }
    }

    /// <summary>
    /// Renders stroke using path geometry with variable width outline.
    /// </summary>
    private void RenderStrokeWithPathGeometry(ID2D1DeviceContext context, Stroke stroke)
    {
        var brush = GetOrCreateBrush(context, stroke.Color);
        var strokeStyle = _deviceManager.RoundStrokeStyle;
        var points = stroke.PointsSpan;

        // First pass: draw line segments
        for (int i = 1; i < points.Length; i++)
        {
            ref readonly var p0 = ref points[i - 1];
            ref readonly var p1 = ref points[i];

            float thickness = CalculateThicknessRef(in p0, in p1, stroke.Thickness);

            context.DrawLine(
                p0.Position,
                p1.Position,
                brush,
                thickness,
                strokeStyle);
        }

        // Second pass: draw circles at each point to fill gaps
        foreach (ref readonly var point in points)
        {
            float thickness = CalculateThicknessForPointRef(in point, stroke.Thickness);
            float radius = thickness * 0.5f;

            var ellipse = new Ellipse(point.Position, radius, radius);
            context.FillEllipse(ellipse, brush);
        }
    }

    /// <summary>
    /// Renders stroke using Catmull-Rom spline interpolation for smooth curves.
    /// </summary>
    private void RenderStrokeWithCatmullRom(ID2D1DeviceContext context, Stroke stroke)
    {
        var brush = GetOrCreateBrush(context, stroke.Color);
        var strokeStyle = _deviceManager.RoundStrokeStyle;

        var points = stroke.PointsSpan;

        // Need at least 2 points for any rendering
        if (points.Length < 2)
            return;

        const int subdivisions = 4;

        var interpolatedPoints = new List<(Vector2 Position, float Thickness)>();

        for (int i = 0; i < points.Length - 1; i++)
        {
            ref readonly var p0 = ref points[Math.Max(0, i - 1)];
            ref readonly var p1 = ref points[i];
            ref readonly var p2 = ref points[i + 1];
            ref readonly var p3 = ref points[Math.Min(points.Length - 1, i + 2)];

            float t1 = CalculateThicknessForPointRef(in p1, stroke.Thickness);
            float t2 = CalculateThicknessForPointRef(in p2, stroke.Thickness);

            if (i == 0)
            {
                interpolatedPoints.Add((p1.Position, t1));
            }

            for (int j = 1; j <= subdivisions; j++)
            {
                float t = j / (float)subdivisions;
                var position = CatmullRom(p0.Position, p1.Position, p2.Position, p3.Position, t);
                float thickness = t1 + (t2 - t1) * t;
                interpolatedPoints.Add((position, thickness));
            }
        }

        // Draw the interpolated curve
        for (int i = 1; i < interpolatedPoints.Count; i++)
        {
            var prev = interpolatedPoints[i - 1];
            var curr = interpolatedPoints[i];

            float avgThickness = (prev.Thickness + curr.Thickness) * 0.5f;
            context.DrawLine(prev.Position, curr.Position, brush, avgThickness, strokeStyle);
        }

        // Fill gaps with circles at key points
        foreach (ref readonly var point in points)
        {
            float thickness = CalculateThicknessForPointRef(in point, stroke.Thickness);
            float radius = thickness * 0.5f;

            var ellipse = new Ellipse(point.Position, radius, radius);
            context.FillEllipse(ellipse, brush);
        }
    }

    /// <summary>
    /// Catmull-Rom spline interpolation.
    /// </summary>
    private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        float b0 = -0.5f * t3 + t2 - 0.5f * t;
        float b1 = 1.5f * t3 - 2.5f * t2 + 1.0f;
        float b2 = -1.5f * t3 + 2.0f * t2 + 0.5f * t;
        float b3 = 0.5f * t3 - 0.5f * t2;

        return new Vector2(
            b0 * p0.X + b1 * p1.X + b2 * p2.X + b3 * p3.X,
            b0 * p0.Y + b1 * p1.Y + b2 * p2.Y + b3 * p3.Y);
    }

    private static float CalculateThickness(StrokePoint p0, StrokePoint p1, float baseThickness)
    {
        float avgPressure = (p0.Pressure + p1.Pressure) * 0.5f;

        float avgTiltX = (p0.TiltX + p1.TiltX) * 0.5f;
        float avgTiltY = (p0.TiltY + p1.TiltY) * 0.5f;
        float tiltMagnitude = MathF.Sqrt(avgTiltX * avgTiltX + avgTiltY * avgTiltY);
        float tiltFactor = 1.0f + (MathF.Min(tiltMagnitude, 45f) * (1f / 45f)) * 0.5f;

        return baseThickness * MathF.Max(0.3f, avgPressure) * tiltFactor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float CalculateThicknessRef(in StrokePoint p0, in StrokePoint p1, float baseThickness)
    {
        float avgPressure = (p0.Pressure + p1.Pressure) * 0.5f;

        float avgTiltX = (p0.TiltX + p1.TiltX) * 0.5f;
        float avgTiltY = (p0.TiltY + p1.TiltY) * 0.5f;
        float tiltMagnitude = MathF.Sqrt(avgTiltX * avgTiltX + avgTiltY * avgTiltY);
        float tiltFactor = 1.0f + (MathF.Min(tiltMagnitude, 45f) * (1f / 45f)) * 0.5f;

        return baseThickness * MathF.Max(0.3f, avgPressure) * tiltFactor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float CalculateThicknessForPointRef(in StrokePoint point, float baseThickness)
    {
        float tiltMagnitude = MathF.Sqrt(point.TiltX * point.TiltX + point.TiltY * point.TiltY);
        float tiltFactor = 1.0f + (MathF.Min(tiltMagnitude, 45f) * (1f / 45f)) * 0.5f;

        return baseThickness * MathF.Max(0.3f, point.Pressure) * tiltFactor;
    }

    private void RenderDot(ID2D1DeviceContext context, StrokePoint point, Stroke stroke)
    {
        var brush = GetOrCreateBrush(context, stroke.Color);

        float tiltMagnitude = MathF.Sqrt(point.TiltX * point.TiltX + point.TiltY * point.TiltY);
        float tiltFactor = 1.0f + (MathF.Min(tiltMagnitude, 45f) * (1f / 45f)) * 0.5f;

        float radius = stroke.Thickness * MathF.Max(0.3f, point.Pressure) * tiltFactor * 0.5f;
        var ellipse = new Ellipse(point.Position, radius, radius);

        context.FillEllipse(ellipse, brush);
    }

    private ID2D1SolidColorBrush GetOrCreateBrush(ID2D1DeviceContext context, StrokeColor color)
    {
        uint key = (uint)((color.A << 24) | (color.R << 16) | (color.G << 8) | color.B);

        if (_brushCache.TryGetValue(key, out var brush))
        {
            return brush;
        }

        var d2dColor = new Color4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
        brush = context.CreateSolidColorBrush(d2dColor);
        _brushCache[key] = brush;

        return brush;
    }

    private ID2D1SolidColorBrush GetOrCreateBrush(ID2D1DeviceContext context, Color4 color)
    {
        byte a = (byte)(color.A * 255);
        byte r = (byte)(color.R * 255);
        byte g = (byte)(color.G * 255);
        byte b = (byte)(color.B * 255);
        uint key = (uint)((a << 24) | (r << 16) | (g << 8) | b);

        if (_brushCache.TryGetValue(key, out var brush))
        {
            return brush;
        }

        brush = context.CreateSolidColorBrush(color);
        _brushCache[key] = brush;

        return brush;
    }

    /// <summary>
    /// Clears all cached brushes. Call when device is lost.
    /// </summary>
    public void ClearBrushCache()
    {
        foreach (var brush in _brushCache.Values)
        {
            brush.Dispose();
        }
        _brushCache.Clear();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _staticContentBitmap?.Dispose();
        _staticContentBitmap = null;

        ClearBrushCache();
    }
}
