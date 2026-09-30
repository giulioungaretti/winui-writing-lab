using System.Numerics;
using InkControl.Input;
using inkapp.Core.Models;
using Windows.Foundation;
using Windows.UI.Input.Inking;

namespace InkControl.Models;

/// <summary>
/// Bridges persistent world-space strokes and the native presenter's viewport-space strokes.
/// </summary>
internal static class NativeStrokeAdapter
{
    public static InkStroke CreateNative(Stroke stroke)
    {
        ArgumentNullException.ThrowIfNull(stroke);
        if (stroke.Points.Count == 0)
            throw new ArgumentException("A native stroke needs at least one point.", nameof(stroke));

        var builder = new InkStrokeBuilder();
        builder.SetDefaultDrawingAttributes(new InkDrawingAttributes
        {
            Color = stroke.Color.ToWindowsColor(),
            Size = new Size(stroke.Thickness, stroke.Thickness),
            IgnorePressure = false,
            IgnoreTilt = false
        });

        // Imported timestamps belong to a previous wall-clock/boot epoch. Keep them in INKS,
        // not in the native modeler's boot-relative timing stream.
        return builder.CreateStrokeFromInkPoints(
            stroke.Points.Select(p => new InkPoint(
                new Point(p.Position.X, p.Position.Y), p.Pressure, p.TiltX, p.TiltY, 0)),
            Matrix3x2.Identity);
    }

    public static Stroke Capture(InkStroke native, ViewportState captureViewport, long capturedAtTicks)
    {
        var attributes = native.DrawingAttributes;
        var points = native.GetInkPoints();
        var lastTimestamp = points.Count == 0 ? 0 : points.Max(p => p.Timestamp);
        var previousTimestamp = points.FirstOrDefault(p => p.Timestamp != 0)?.Timestamp ?? 0;
        var stroke = new Stroke(
            attributes.Color.ToStrokeColor(),
            (float)attributes.Size.Width / captureViewport.Zoom);

        foreach (var point in points)
        {
            var screen = Vector2.Transform(
                new Vector2((float)point.Position.X, (float)point.Position.Y), native.PointTransform);
            // Native timestamps are microseconds since boot; INKS uses DateTime ticks.
            // A zero timestamp means unspecified, not system boot. Carry the previous
            // reported time rather than introducing an uptime-sized jump into INKS.
            var timestamp = point.Timestamp == 0 ? previousTimestamp : point.Timestamp;
            previousTimestamp = timestamp;
            var elapsedTicks = checked((long)(lastTimestamp - timestamp) * 10);
            stroke.AddPoint(new StrokePoint(
                captureViewport.ScreenToWorld(screen), point.Pressure, point.TiltX, point.TiltY,
                checked(capturedAtTicks - elapsedTicks)));
        }

        return stroke;
    }

    public static void Project(InkStroke native, Matrix3x2 sourceToWorld, Stroke stroke, ViewportState viewport)
    {
        // PointTransform does not scale the pen tip. Scale its size explicitly, always from
        // the world-space model rather than accumulating previous view transformations.
        native.PointTransform = sourceToWorld * viewport.GetTransformMatrix();
        var attributes = native.DrawingAttributes;
        attributes.Size = new Size(stroke.Thickness * viewport.Zoom, stroke.Thickness * viewport.Zoom);
        attributes.Color = stroke.Color.ToWindowsColor();
        native.DrawingAttributes = attributes;
    }
}
