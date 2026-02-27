using System.Text;
using Devlabs.AcTiming.Application.Shared;
using Devlabs.AcTiming.Domain.LiveTiming;
using Microsoft.AspNetCore.Components;

namespace Devlabs.AcTiming.Web.Components.Pages.LapComparison;

public partial class LapComparisonPage
{
    [Inject]
    private IAiChat AiChat { get; set; } = default!;

    // ── AI state ───────────────────────────────────────────────────────────────

    private bool _aiAnalyzing;
    private string? _aiResponse;
    private string? _aiError;
    private CancellationTokenSource? _aiCts;

    private bool CanAskAi =>
        AiChat.IsEnabled
        && !string.IsNullOrEmpty(_keyA)
        && !string.IsNullOrEmpty(_keyB)
        && !_aiAnalyzing;

    private async Task AskAiAsync()
    {
        var lapA = SelectedLap(_keyA);
        var lapB = SelectedLap(_keyB);
        if (lapA is null || lapB is null)
            return;

        var grid = GetCachedGrid(lapA, lapB);

        _aiCts?.Cancel();
        _aiCts?.Dispose();
        _aiCts = new CancellationTokenSource();

        _aiAnalyzing = true;
        _aiResponse = null;
        _aiError = null;
        StateHasChanged();

        try
        {
            var prompt = BuildAnalysisPrompt(lapA, lapB, grid);
            _aiResponse = await AiChat.AskQuestionAsync(SystemPrompt, prompt, _aiCts.Token);
        }
        catch (OperationCanceledException)
        { /* user navigated away */
        }
        catch (Exception ex)
        {
            _aiError = $"AI analysis failed: {ex.Message}";
        }
        finally
        {
            _aiAnalyzing = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    // ── System prompt ──────────────────────────────────────────────────────────

    private const string SystemPrompt =
        "You are an expert motorsport data engineer and driver coach with deep experience in Assetto Corsa. "
        + "You receive pre-processed telemetry data (speed, gear, sector delta, corner detection). "
        + "Analyze the data and give specific, actionable lap time coaching advice for the slower driver. "
        + "Be concise, precise, and direct. Reference track positions as percentages. "
        + "Focus on where time is actually lost and provide concrete guidance on what to change.";

    // ── Prompt builder ─────────────────────────────────────────────────────────

    private static string BuildAnalysisPrompt(
        BestLapTelemetry lapA,
        BestLapTelemetry lapB,
        GridPoint[] grid
    )
    {
        var sb = new StringBuilder();

        var totalDeltaMs = lapB.LapTimeMs - lapA.LapTimeMs;
        var fasterName = totalDeltaMs > 0 ? lapA.DriverName : lapB.DriverName;
        var slowerName = totalDeltaMs > 0 ? lapB.DriverName : lapA.DriverName;
        var deltaSec = Math.Abs(totalDeltaMs / 1000.0);

        float trackLengthM = EstimateTrackLengthMeters(
            lapA.Samples.Count >= lapB.Samples.Count ? lapA : lapB
        );

        sb.AppendLine("TELEMETRY COMPARISON:");
        sb.AppendLine(
            $"- {lapA.DriverName}: {lapA.CarModel} | Lap: {FormatLapTime(lapA.LapTimeMs)}"
        );
        sb.AppendLine(
            $"- {lapB.DriverName}: {lapB.CarModel} | Lap: {FormatLapTime(lapB.LapTimeMs)}"
        );
        sb.AppendLine($"- {fasterName} is faster by {deltaSec:F3}s");
        sb.AppendLine($"- Estimated track length: {trackLengthM:F0}m");
        sb.AppendLine();

        AppendSectorAnalysis(sb, grid, lapA.DriverName, lapB.DriverName);
        AppendCornerAnalysis(sb, grid, lapA.DriverName, lapB.DriverName, trackLengthM);
        AppendGearDifferences(sb, grid, lapA.DriverName, lapB.DriverName);

        sb.AppendLine();
        sb.AppendLine(
            $"Give exactly 4-5 numbered coaching points for {slowerName} to improve their lap time."
        );
        sb.AppendLine(
            "Use the driver's actual name. Use meters (m) for distances, seconds (s) for time gaps."
        );
        sb.AppendLine(
            "Be direct and specific — every point must reference a track position (%) and concrete numbers."
        );

        return sb.ToString();
    }

    /// <summary>Approximates track length by summing Euclidean distance between consecutive world-coordinate samples.</summary>
    private static float EstimateTrackLengthMeters(BestLapTelemetry lap)
    {
        var s = lap.Samples;
        float total = 0f;
        for (int i = 1; i < s.Count; i++)
        {
            float dx = s[i].WorldX - s[i - 1].WorldX;
            float dz = s[i].WorldZ - s[i - 1].WorldZ;
            total += MathF.Sqrt(dx * dx + dz * dz);
        }
        return total;
    }

    // ── Sector analysis ────────────────────────────────────────────────────────

    private const int SectorCount = 10;

    private static void AppendSectorAnalysis(
        StringBuilder sb,
        GridPoint[] grid,
        string nameA,
        string nameB
    )
    {
        sb.AppendLine(
            "SECTOR SPLITS (10 equal sectors — time values are the delta gained WITHIN each sector, not cumulative):"
        );

        int sectorSize = grid.Length / SectorCount;

        for (int s = 0; s < SectorCount; s++)
        {
            int from = s * sectorSize;
            int to = s == SectorCount - 1 ? grid.Length - 1 : (s + 1) * sectorSize - 1;

            float fromPct = (float)from / (grid.Length - 1) * 100f;
            float toPct = (float)to / (grid.Length - 1) * 100f;

            float deltaGained =
                (float)(grid[to].DeltaSeconds ?? 0) - (float)(grid[from].DeltaSeconds ?? 0);

            float avgA = AverageSpeed(grid, from, to, p => p.SpeedA);
            float avgB = AverageSpeed(grid, from, to, p => p.SpeedB);

            string deltaDesc =
                Math.Abs(deltaGained) < 0.005f ? "even"
                : deltaGained > 0 ? $"{nameB} gains {deltaGained:F3}s"
                : $"{nameA} gains {-deltaGained:F3}s";

            sb.AppendLine(
                $"  {fromPct:F0}–{toPct:F0}%: {nameA} avg {avgA:F0} km/h, {nameB} avg {avgB:F0} km/h | {deltaDesc}"
            );
        }

        sb.AppendLine();
    }

    private static float AverageSpeed(
        GridPoint[] grid,
        int from,
        int to,
        Func<GridPoint, decimal?> selector
    )
    {
        float sum = 0;
        int cnt = 0;
        for (int i = from; i <= to; i++)
        {
            var v = selector(grid[i]);
            if (v.HasValue && v.Value > 0)
            {
                sum += (float)v.Value;
                cnt++;
            }
        }
        return cnt > 0 ? sum / cnt : 0f;
    }

    // ── Corner analysis ────────────────────────────────────────────────────────

    private static void AppendCornerAnalysis(
        StringBuilder sb,
        GridPoint[] grid,
        string nameA,
        string nameB,
        float trackLengthM
    )
    {
        var corners = DetectCorners(grid);
        if (corners.Count == 0)
        {
            sb.AppendLine("CORNER ANALYSIS: No significant corners detected.");
            return;
        }

        corners.Sort(
            (a, b) => Math.Abs(b.CornerDeltaSwing).CompareTo(Math.Abs(a.CornerDeltaSwing))
        );

        sb.AppendLine(
            "CORNER ANALYSIS (up to 8, sorted by time gained/lost per corner — delta is the swing through the corner, not cumulative):"
        );

        foreach (var c in corners.Take(8))
        {
            var speedA = c.ApexSpeedA.HasValue ? $"{c.ApexSpeedA.Value:F0} km/h" : "—";
            var speedB = c.ApexSpeedB.HasValue ? $"{c.ApexSpeedB.Value:F0} km/h" : "—";

            string brakeDesc = "";
            if (c.BrakingPointA.HasValue && c.BrakingPointB.HasValue)
            {
                float diffPct = c.BrakingPointB.Value - c.BrakingPointA.Value; // positive = B brakes later (closer to apex)
                float diffM = MathF.Abs(diffPct) / 100f * trackLengthM;
                if (diffM >= 3f) // ignore differences under 3m — within noise
                    brakeDesc =
                        diffPct > 0
                            ? $", {nameB} brakes {diffM:F0}m later (closer to apex)"
                            : $", {nameA} brakes {diffM:F0}m later (closer to apex)";
            }
            else if (c.BrakingPointA.HasValue && !c.BrakingPointB.HasValue)
            {
                brakeDesc = $", no clear braking point detected for {nameB}";
            }
            else if (!c.BrakingPointA.HasValue && c.BrakingPointB.HasValue)
            {
                brakeDesc = $", no clear braking point detected for {nameA}";
            }

            string timeDesc =
                Math.Abs(c.CornerDeltaSwing) < 0.005f ? "even through corner"
                : c.CornerDeltaSwing > 0
                    ? $"{nameB} gains {c.CornerDeltaSwing:F3}s through this corner"
                : $"{nameA} gains {-c.CornerDeltaSwing:F3}s through this corner";

            sb.AppendLine(
                $"  Corner ~{c.SplinePercent:F1}%: {nameA} apex {speedA} / {nameB} apex {speedB}{brakeDesc} | {timeDesc}"
            );
        }

        sb.AppendLine();
    }

    // CornerDeltaSwing: time B gained vs A across the corner window (braking entry → corner exit).
    // Positive = B faster through this corner; negative = A faster.
    // This is a delta swing, NOT a cumulative running delta.
    private sealed record CornerInfo(
        float SplinePercent,
        float? ApexSpeedA,
        float? ApexSpeedB,
        float? BrakingPointA,
        float? BrakingPointB,
        float CornerDeltaSwing
    );

    private static List<CornerInfo> DetectCorners(GridPoint[] grid)
    {
        if (grid.Length < 30)
            return [];

        float[] smoothA = SmoothSpeeds(grid, p => p.SpeedA);
        float[] smoothB = SmoothSpeeds(grid, p => p.SpeedB);

        // Reference trace for corner detection — average available channels
        float[] reference = new float[grid.Length];
        for (int i = 0; i < grid.Length; i++)
        {
            float a = smoothA[i],
                b = smoothB[i];
            reference[i] = a > 0 && b > 0 ? (a + b) * 0.5f : Math.Max(a, b);
        }

        const int windowHalf = 15; // grid points on each side to find local min
        const float minDepth = 20f; // minimum km/h drop to qualify as a corner

        var corners = new List<CornerInfo>();

        for (int i = windowHalf; i < grid.Length - windowHalf; i++)
        {
            float cur = reference[i];
            if (cur <= 0)
                continue;

            // Must be a local minimum within the half-window
            bool isMin = true;
            for (int w = 1; w <= windowHalf; w++)
            {
                if (reference[i - w] <= cur || reference[i + w] <= cur)
                {
                    isMin = false;
                    break;
                }
            }
            if (!isMin)
                continue;

            // Check depth against surrounding speed range
            int lookback = Math.Min(80, i);
            int lookahead = Math.Min(80, grid.Length - 1 - i);

            float peakBefore = 0f,
                peakAfter = 0f;
            for (int w = 1; w <= lookback; w++)
                peakBefore = Math.Max(peakBefore, reference[i - w]);
            for (int w = 1; w <= lookahead; w++)
                peakAfter = Math.Max(peakAfter, reference[i + w]);

            float depth = Math.Max(peakBefore - cur, peakAfter - cur);
            if (depth < minDepth)
                continue;

            float splinePct = (float)i / (grid.Length - 1) * 100f;
            int? brakeIdxA = FindBrakingPoint(smoothA, i);
            int? brakeIdxB = FindBrakingPoint(smoothB, i);

            // Delta swing across the full corner window: entry (braking zone start) → exit.
            // Use the earlier braking point as window start; fall back to 30 grid pts before apex.
            int windowEntry = Math.Max(0, Math.Min(brakeIdxA ?? i - 30, brakeIdxB ?? i - 30));
            int windowExit = Math.Min(grid.Length - 1, i + 25);
            float cornerDeltaSwing =
                (float)(grid[windowExit].DeltaSeconds ?? 0)
                - (float)(grid[windowEntry].DeltaSeconds ?? 0);

            corners.Add(
                new CornerInfo(
                    SplinePercent: splinePct,
                    ApexSpeedA: smoothA[i] > 0 ? smoothA[i] : null,
                    ApexSpeedB: smoothB[i] > 0 ? smoothB[i] : null,
                    BrakingPointA: brakeIdxA.HasValue
                        ? (float)brakeIdxA.Value / (grid.Length - 1) * 100f
                        : null,
                    BrakingPointB: brakeIdxB.HasValue
                        ? (float)brakeIdxB.Value / (grid.Length - 1) * 100f
                        : null,
                    CornerDeltaSwing: cornerDeltaSwing
                )
            );
        }

        return MergeNearbyCorners(corners, thresholdPct: 4f);
    }

    /// <summary>5-point moving average over speed values; returns 0 for missing data.</summary>
    private static float[] SmoothSpeeds(GridPoint[] grid, Func<GridPoint, decimal?> selector)
    {
        var result = new float[grid.Length];
        const int halfW = 5;

        for (int i = 0; i < grid.Length; i++)
        {
            float sum = 0;
            int cnt = 0;
            for (int j = Math.Max(0, i - halfW); j <= Math.Min(grid.Length - 1, i + halfW); j++)
            {
                var v = selector(grid[j]);
                if (v.HasValue && v.Value > 0)
                {
                    sum += (float)v.Value;
                    cnt++;
                }
            }
            result[i] = cnt > 0 ? sum / cnt : 0f;
        }
        return result;
    }

    /// <summary>
    /// Walk backwards from the apex; return the last local speed maximum (= end of full throttle, start of braking).
    /// </summary>
    private static int? FindBrakingPoint(float[] smooth, int apexIdx)
    {
        if (smooth[apexIdx] <= 0)
            return null;

        int maxBack = Math.Min(apexIdx, 80);

        for (int i = apexIdx - 2; i >= apexIdx - maxBack; i--)
        {
            if (smooth[i] <= 0)
                continue;
            // Local maximum: higher than both neighbors → transition from rising to falling
            if (smooth[i] > smooth[i - 1] && smooth[i] >= smooth[i + 1])
                return i;
        }
        return null;
    }

    /// <summary>Merge corners within <paramref name="thresholdPct"/> of each other, keeping the deeper one.</summary>
    private static List<CornerInfo> MergeNearbyCorners(List<CornerInfo> corners, float thresholdPct)
    {
        if (corners.Count == 0)
            return corners;

        corners.Sort((a, b) => a.SplinePercent.CompareTo(b.SplinePercent));

        var merged = new List<CornerInfo> { corners[0] };

        for (int i = 1; i < corners.Count; i++)
        {
            var prev = merged[^1];
            var cur = corners[i];

            if (cur.SplinePercent - prev.SplinePercent < thresholdPct)
            {
                // Keep the one with lower apex speed (tighter corner = more important)
                float prevMin = Math.Min(
                    prev.ApexSpeedA ?? float.MaxValue,
                    prev.ApexSpeedB ?? float.MaxValue
                );
                float curMin = Math.Min(
                    cur.ApexSpeedA ?? float.MaxValue,
                    cur.ApexSpeedB ?? float.MaxValue
                );
                if (curMin < prevMin)
                    merged[^1] = cur;
            }
            else
                merged.Add(cur);
        }

        return merged;
    }

    // ── Gear strategy differences ──────────────────────────────────────────────

    private static void AppendGearDifferences(
        StringBuilder sb,
        GridPoint[] grid,
        string nameA,
        string nameB
    )
    {
        var diffs = new List<(float FromPct, float ToPct, int GearA, int GearB)>();

        int? segStart = null;
        int lastGearA = -1,
            lastGearB = -1;

        for (int i = 0; i < grid.Length; i++)
        {
            int gA = grid[i].GearA ?? -1;
            int gB = grid[i].GearB ?? -1;

            if (gA <= 0 || gB <= 0)
            {
                segStart = null;
                continue;
            }

            if (gA != gB)
            {
                segStart ??= i;
                lastGearA = gA;
                lastGearB = gB;
            }
            else
            {
                if (segStart.HasValue)
                {
                    float lengthPct = (float)(i - segStart.Value) / grid.Length * 100f;
                    if (lengthPct >= 1f)
                        diffs.Add(
                            (
                                (float)segStart.Value / (grid.Length - 1) * 100f,
                                (float)i / (grid.Length - 1) * 100f,
                                lastGearA,
                                lastGearB
                            )
                        );

                    segStart = null;
                }
            }
        }

        if (diffs.Count == 0)
        {
            sb.AppendLine("GEAR STRATEGY: Identical gear usage throughout.");
        }
        else
        {
            sb.AppendLine("GEAR DIFFERENCES (segments lasting ≥1% of lap):");
            foreach (var (from, to, gA, gB) in diffs.Take(6))
            {
                string who =
                    gA > gB
                        ? $"{nameA} uses gear {gA}, {nameB} uses gear {gB} ({nameA} one gear higher)"
                        : $"{nameB} uses gear {gB}, {nameA} uses gear {gA} ({nameB} one gear higher)";
                sb.AppendLine($"  {from:F0}–{to:F0}%: {who}");
            }
        }
    }
}
