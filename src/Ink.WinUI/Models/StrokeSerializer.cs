using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using inkapp.Core.Models;

namespace InkControl.Models;

/// <summary>
/// Serializes and deserializes stroke data to/from binary format.
/// Format is compact and designed for fast read/write.
/// </summary>
internal static class StrokeSerializer
{
    // File format version for future compatibility
    // Version 1: Original format (position, pressure, timestamp)
    // Version 2: Added tilt support (tiltX, tiltY per point)
    private const byte FormatVersion = 2;

    // Magic bytes to identify our format
    private static readonly byte[] MagicBytes = [(byte)'I', (byte)'N', (byte)'K', (byte)'S'];

    /// <summary>
    /// Serializes a stroke collection to binary format.
    /// </summary>
    /// <returns>Binary data representing the strokes, or empty array if no strokes.</returns>
    public static byte[] Serialize(StrokeCollection strokes)
    {
        ArgumentNullException.ThrowIfNull(strokes);

        if (strokes.Strokes.Count == 0)
            return [];

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write header
        writer.Write(MagicBytes);
        writer.Write(FormatVersion);
        writer.Write(strokes.Strokes.Count);

        // Write each stroke
        foreach (var stroke in strokes.Strokes)
        {
            WriteStroke(writer, stroke);
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Deserializes binary data to a stroke collection.
    /// </summary>
    /// <returns>A new StrokeCollection, or empty collection if data is invalid.</returns>
    public static StrokeCollection Deserialize(byte[] data)
    {
        var collection = new StrokeCollection();

        if (data is null || data.Length < 9) // Minimum: 4 magic + 1 version + 4 count
            return collection;

        try
        {
            using var ms = new MemoryStream(data);
            using var reader = new BinaryReader(ms);

            // Verify magic bytes
            var magic = reader.ReadBytes(4);
            if (!magic.AsSpan().SequenceEqual(MagicBytes))
                return collection;

            // Check version
            var version = reader.ReadByte();
            if (version > FormatVersion)
                return collection; // Unknown future version

            // Read stroke count
            var strokeCount = reader.ReadInt32();
            if (strokeCount < 0 || strokeCount > 100000) // Sanity check
                return collection;

            // Read strokes
            for (int i = 0; i < strokeCount; i++)
            {
                var stroke = ReadStroke(reader, version);
                if (stroke is not null)
                {
                    collection.Add(stroke);
                }
            }
        }
        catch (EndOfStreamException)
        {
            // Truncated data - return what we have
        }
        catch (IOException)
        {
            // Corrupted data - return what we have
        }

        collection.MarkClean();
        return collection;
    }

    private static void WriteStroke(BinaryWriter writer, Stroke stroke)
    {
        // Write stroke ID (16 bytes)
        writer.Write(stroke.Id.ToByteArray());

        // Write color (4 bytes: A, R, G, B)
        writer.Write(stroke.Color.A);
        writer.Write(stroke.Color.R);
        writer.Write(stroke.Color.G);
        writer.Write(stroke.Color.B);

        // Write thickness (4 bytes)
        writer.Write(stroke.Thickness);

        // Write point count (4 bytes)
        writer.Write(stroke.Points.Count);

        // Write points (28 bytes each: X, Y, Pressure, TiltX, TiltY, Timestamp)
        foreach (var point in stroke.Points)
        {
            writer.Write(point.Position.X);
            writer.Write(point.Position.Y);
            writer.Write(point.Pressure);
            writer.Write(point.TiltX);
            writer.Write(point.TiltY);
            writer.Write(point.TimestampTicks);
        }
    }

    private static Stroke? ReadStroke(BinaryReader reader, byte version)
    {
        // Read stroke ID
        var idBytes = reader.ReadBytes(16);
        var id = new Guid(idBytes);

        // Read color
        var a = reader.ReadByte();
        var r = reader.ReadByte();
        var g = reader.ReadByte();
        var b = reader.ReadByte();
        var color = StrokeColor.FromArgb(a, r, g, b);

        // Read thickness
        var thickness = reader.ReadSingle();
        if (thickness <= 0 || thickness > 100) // Sanity check
            return null;

        // Read point count
        var pointCount = reader.ReadInt32();
        if (pointCount < 0 || pointCount > 100000) // Sanity check
            return null;

        // Read points - format depends on version
        var points = new List<StrokePoint>(pointCount);
        for (int i = 0; i < pointCount; i++)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var pressure = reader.ReadSingle();

            float tiltX = 0f;
            float tiltY = 0f;

            // Version 2+ includes tilt data
            if (version >= 2)
            {
                tiltX = reader.ReadSingle();
                tiltY = reader.ReadSingle();
            }

            var timestamp = reader.ReadInt64();

            points.Add(new StrokePoint(new Vector2(x, y), pressure, tiltX, tiltY, timestamp));
        }

        return new Stroke(id, points, color, thickness);
    }
}
