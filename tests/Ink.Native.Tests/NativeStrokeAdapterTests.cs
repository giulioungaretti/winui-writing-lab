using System.Numerics;
using InkControl.Models;
using inkapp.Core.Models;
using Windows.Foundation;
using Windows.UI.Input.Inking;
using Xunit;

namespace Ink.Native.Tests;

public class NativeStrokeAdapterTests
{
    private static Stroke CreateWorldStroke()
    {
        var stroke = new Stroke(StrokeColor.FromArgb(255, 32, 64, 128), 4);
        stroke.AddPoint(new StrokePoint(new Vector2(-100, 50), 0.4f, 15, -10, 638000000000000000));
        stroke.AddPoint(new StrokePoint(new Vector2(100, 80), 0.8f, -5, 20, 638000000000100000));
        return stroke;
    }

    [Theory]
    [InlineData(0.25f, 1200, -500)]
    [InlineData(1f, -50000, 40000)]
    [InlineData(8f, 450, -300)]
    public void ProjectionPreservesWorldPointsAndScalesPenWidth(float zoom, float panX, float panY)
    {
        var model = CreateWorldStroke();
        var native = NativeStrokeAdapter.CreateNative(model);
        var viewport = new ViewportState { Pan = new Vector2(panX, panY), Zoom = zoom };

        NativeStrokeAdapter.Project(native, Matrix3x2.Identity, model, viewport);

        Assert.Equal(model.Thickness * zoom, native.DrawingAttributes.Size.Width, 4);
        var rawPoints = native.GetInkPoints();
        for (var i = 0; i < rawPoints.Count; i++)
        {
            var raw = new Vector2((float)rawPoints[i].Position.X, (float)rawPoints[i].Position.Y);
            Assert.Equal(model.Points[i].Position, raw);
            Assert.Equal(viewport.WorldToScreen(raw), Vector2.Transform(raw, native.PointTransform));
            Assert.Equal(model.Points[i].Pressure, rawPoints[i].Pressure, 4);
            Assert.Equal(model.Points[i].TiltX, rawPoints[i].TiltX, 4);
        }
    }

    [Fact]
    public void RepeatedNavigationDoesNotAccumulateTransformOrWidthErrors()
    {
        var model = CreateWorldStroke();
        var native = NativeStrokeAdapter.CreateNative(model);
        for (var i = 0; i < 100; i++)
        {
            NativeStrokeAdapter.Project(native, Matrix3x2.Identity, model,
                new ViewportState { Pan = new Vector2(i * 1000, -i * 2000), Zoom = 8 });
            NativeStrokeAdapter.Project(native, Matrix3x2.Identity, model, ViewportState.Default);
        }
        Assert.Equal(Matrix3x2.Identity, native.PointTransform);
        Assert.Equal(model.Thickness, native.DrawingAttributes.Size.Width, 4);
        Assert.Equal(new Point(-100, 50), native.GetInkPoints()[0].Position);
    }

    [Fact]
    public void CapturedStrokeCanBeReprojectedWithoutRebuildingNativeInk()
    {
        var captureView = new ViewportState { Pan = new Vector2(350, 200), Zoom = 2 };
        var builder = new InkStrokeBuilder();
        builder.SetDefaultDrawingAttributes(new InkDrawingAttributes { Size = new Size(8, 8) });
        var native = builder.CreateStrokeFromInkPoints(
            new[]
            {
                new InkPoint(new Point(100, 150), 0.4f, 10, -5, 1000),
                new InkPoint(new Point(150, 175), 0.8f, 20, 15, 2000)
            }, Matrix3x2.Identity);
        const long now = 638000000000000000;
        var model = NativeStrokeAdapter.Capture(native, captureView, now);
        Assert.Equal(new Vector2(-125, -25), model.Points[0].Position);
        Assert.Equal(4, model.Thickness);
        Assert.Equal(now - 10000, model.Points[0].TimestampTicks);
        Assert.Equal(now, model.Points[1].TimestampTicks);

        var destination = new ViewportState { Pan = new Vector2(-200, 450), Zoom = 0.5f };
        NativeStrokeAdapter.Project(native, captureView.GetInverseTransformMatrix(), model, destination);
        var recaptured = NativeStrokeAdapter.Capture(native, destination, now);
        Assert.Equal(model.Points, recaptured.Points);
        Assert.Equal(model.Thickness, recaptured.Thickness);
    }

    [Fact]
    public void NavigationDoesNotChangeInksPersistenceOrGuids()
    {
        var model = CreateWorldStroke();
        var collection = new StrokeCollection();
        collection.Add(model);
        var before = StrokeSerializer.Serialize(collection);
        var native = NativeStrokeAdapter.CreateNative(model);
        NativeStrokeAdapter.Project(native, Matrix3x2.Identity, model,
            new ViewportState { Pan = new Vector2(-10000, 20000), Zoom = 8 });
        Assert.Equal(before, StrokeSerializer.Serialize(collection));
        var reloaded = StrokeSerializer.Deserialize(before).Strokes.Single();
        Assert.Equal(model.Id, reloaded.Id);
        Assert.Equal(model.Points, reloaded.Points);
        Assert.Equal(model.Color, reloaded.Color);
        Assert.Equal(model.Thickness, reloaded.Thickness);
    }

    [Fact]
    public void NativeProjectionSupportsSmallestPenAtMinimumZoom()
    {
        var model = CreateWorldStroke();
        model.Thickness = 0.1f;
        var native = NativeStrokeAdapter.CreateNative(model);
        NativeStrokeAdapter.Project(native, Matrix3x2.Identity, model,
            new ViewportState { Zoom = ViewportState.MinZoom });
        Assert.Equal(model.Thickness * ViewportState.MinZoom, native.DrawingAttributes.Size.Width, 4);
    }

    [Fact]
    public void NativeProjectionSupportsLargestPenAtMaximumZoom()
    {
        var model = CreateWorldStroke();
        model.Thickness = 100;
        var native = NativeStrokeAdapter.CreateNative(model);
        NativeStrokeAdapter.Project(native, Matrix3x2.Identity, model,
            new ViewportState { Zoom = ViewportState.MaxZoom });
        Assert.Equal(model.Thickness * ViewportState.MaxZoom, native.DrawingAttributes.Size.Width, 4);
    }

    [Fact]
    public void MissingNativeTimestampsDoNotIntroduceBootTimeJumps()
    {
        var builder = new InkStrokeBuilder();
        var native = builder.CreateStrokeFromInkPoints(
            new[]
            {
                new InkPoint(new Point(10, 10), 0.5f, 0, 0, 1000),
                new InkPoint(new Point(20, 20), 0.5f, 0, 0, 0),
                new InkPoint(new Point(30, 30), 0.5f, 0, 0, 2000)
            }, Matrix3x2.Identity);
        const long now = 638000000000000000;
        var model = NativeStrokeAdapter.Capture(native, ViewportState.Default, now);
        Assert.Equal(now - 10000, model.Points[0].TimestampTicks);
        Assert.Equal(model.Points[0].TimestampTicks, model.Points[1].TimestampTicks);
        Assert.Equal(now, model.Points[2].TimestampTicks);
    }

    [Fact]
    public void NativeStrokeIdsCanKeyThePresentationCacheBeforeInsertion()
    {
        var first = NativeStrokeAdapter.CreateNative(CreateWorldStroke());
        var second = NativeStrokeAdapter.CreateNative(CreateWorldStroke());
        Assert.NotEqual(first.Id, second.Id);
        var firstId = first.Id;
        var container = new InkStrokeContainer();
        container.AddStrokes(new[] { first, second });
        Assert.Equal(firstId, first.Id);
        Assert.Equal(2, container.GetStrokes().Count);
    }

    [Fact]
    public void NativeEraserHitGeometryFollowsProjectedPosition()
    {
        var model = CreateWorldStroke();
        var native = NativeStrokeAdapter.CreateNative(model);
        var view = new ViewportState { Pan = new Vector2(1000, 500), Zoom = 2 };
        NativeStrokeAdapter.Project(native, Matrix3x2.Identity, model, view);
        var container = new InkStrokeContainer();
        container.AddStroke(native);
        container.SelectWithLine(new Point(800, 550), new Point(800, 650));
        Assert.True(native.Selected);
        container.DeleteSelected();
        Assert.Empty(container.GetStrokes());
    }

    [Fact]
    public void InksVersionOneStillImportsWithoutTilt()
    {
        var id = Guid.NewGuid();
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
        {
            writer.Write(new byte[] { (byte)'I', (byte)'N', (byte)'K', (byte)'S', 1 });
            writer.Write(1);
            writer.Write(id.ToByteArray());
            writer.Write(new byte[] { 255, 0, 0, 0 });
            writer.Write(2f);
            writer.Write(1);
            writer.Write(-50f);
            writer.Write(100f);
            writer.Write(0.75f);
            writer.Write(638000000000000000L);
        }
        var model = StrokeSerializer.Deserialize(stream.ToArray()).Strokes.Single();
        Assert.Equal(id, model.Id);
        var point = Assert.Single(model.Points);
        Assert.Equal(0, point.TiltX);
        Assert.Equal(0, point.TiltY);
        Assert.Single(NativeStrokeAdapter.CreateNative(model).GetInkPoints());
    }
}
