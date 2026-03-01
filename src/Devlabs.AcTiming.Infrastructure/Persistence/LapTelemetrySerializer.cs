using System.Buffers.Binary;
using Devlabs.AcTiming.Domain.LiveTiming;

namespace Devlabs.AcTiming.Infrastructure.Persistence;

/// <summary>
/// Encodes/decodes <see cref="LapTelemetrySample"/> lists as a compact binary blob.
/// <br/>
/// Wire format (little-endian):
/// <code>
/// [int32 count] [float32 SplinePos] [float32 WorldX] [float32 WorldZ] [float32 SpeedKmh] [int32 Gear] × count
/// </code>
/// Fixed record size: 4 header + N × 20 bytes.
/// At 5 000 samples: 100 100 bytes ≈ 98 KB.
/// </summary>
internal static class LapTelemetrySerializer
{
    private const int RecordSize = 20; // 4 floats + 1 int, each 4 bytes

    public static byte[] Serialize(List<LapTelemetrySample> samples)
    {
        var buffer = new byte[4 + samples.Count * RecordSize];
        var span = buffer.AsSpan();

        BinaryPrimitives.WriteInt32LittleEndian(span, samples.Count);
        span = span[4..];

        foreach (var s in samples)
        {
            BinaryPrimitives.WriteSingleLittleEndian(span, s.SplinePosition);
            BinaryPrimitives.WriteSingleLittleEndian(span[4..], s.WorldX);
            BinaryPrimitives.WriteSingleLittleEndian(span[8..], s.WorldZ);
            BinaryPrimitives.WriteSingleLittleEndian(span[12..], s.SpeedKmh);
            BinaryPrimitives.WriteInt32LittleEndian(span[16..], s.Gear);
            span = span[RecordSize..];
        }

        return buffer;
    }

    public static List<LapTelemetrySample> Deserialize(byte[] data)
    {
        if (data is not { Length: >= 4 })
            return [];

        var span = data.AsSpan();
        var count = BinaryPrimitives.ReadInt32LittleEndian(span);

        if (count <= 0 || data.Length < 4 + count * RecordSize)
            return [];

        var samples = new List<LapTelemetrySample>(count);
        span = span[4..];

        for (var i = 0; i < count; i++)
        {
            samples.Add(
                new LapTelemetrySample(
                    SplinePosition: BinaryPrimitives.ReadSingleLittleEndian(span),
                    WorldX: BinaryPrimitives.ReadSingleLittleEndian(span[4..]),
                    WorldZ: BinaryPrimitives.ReadSingleLittleEndian(span[8..]),
                    SpeedKmh: BinaryPrimitives.ReadSingleLittleEndian(span[12..]),
                    Gear: BinaryPrimitives.ReadInt32LittleEndian(span[16..])
                )
            );
            span = span[RecordSize..];
        }

        return samples;
    }
}
