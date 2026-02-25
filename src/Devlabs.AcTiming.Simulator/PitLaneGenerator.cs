/// <summary>
/// Generates a pit_lane.ai binary file matching the format expected by PitLaneSplineLoader.
/// Pit lane: vertical line at X=500, from Z=200 (entry) to Z=-200 (exit).
/// </summary>
public static class PitLaneGenerator
{
    public static void Generate(string outputPath, int pointCount = 50)
    {
        const float pitX = 500f;
        const float pitEntryZ = 200f;
        const float pitExitZ = -200f;

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        using var fs = File.Create(outputPath);
        using var bw = new BinaryWriter(fs);

        // 24-byte header: version(4) + count(4) + padding(16)
        bw.Write(7); // version
        bw.Write(pointCount); // count
        bw.Write(new byte[16]); // padding

        var cumDist = 0f;
        var prevZ = pitEntryZ;

        for (var i = 0; i < pointCount; i++)
        {
            var t = i / (float)(pointCount - 1);
            var worldX = pitX;
            var worldZ = pitEntryZ + t * (pitExitZ - pitEntryZ);

            if (i > 0)
                cumDist += MathF.Abs(worldZ - prevZ);

            // Binary format per PitLaneSplineLoader:
            // float worldZ, float cumDist, float unused, float worldX, float elevY
            bw.Write(worldZ); // f[0]
            bw.Write(cumDist); // f[1]
            bw.Write(0f); // f[2] unused
            bw.Write(worldX); // f[3]
            bw.Write(0f); // f[4] elevation

            prevZ = worldZ;
        }

        Console.WriteLine(
            $"  Generated pit_lane.ai: {pointCount} points at X={pitX}, Z=[{pitEntryZ}..{pitExitZ}]"
        );
    }
}
