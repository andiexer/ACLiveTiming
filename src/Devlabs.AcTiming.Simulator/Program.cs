using System.Net;
using System.Net.Sockets;
using Devlabs.AcTiming.Simulator;

var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 9996;
var count = Math.Min(args.Length > 1 && int.TryParse(args[1], out var d) ? d : 10, 10);
var tickMs = args.Length > 2 && int.TryParse(args[2], out var t) ? t : 250;

Console.WriteLine($"AC Timing Simulator - Sending to UDP port {port}");
Console.WriteLine($"Drivers: {count}, Tick: {tickMs}ms");
Console.WriteLine("Press Ctrl+C to stop.");
Console.WriteLine();

using var udp = new UdpClient();
var endpoint = new IPEndPoint(IPAddress.Loopback, port);
var writer = new AcPacketWriter();
var rng = new Random(42);

// 10 GT3 drivers — take the first `count`
(string Name, string Guid, string Car, string Skin)[] allDrivers =
[
    ("Max Verstappen", "S76561198000000001", "ks_ferrari_488_gt3", "red_1"),
    ("Lewis Hamilton", "S76561198000000002", "ks_porsche_911_gt3_r", "white_2"),
    ("Charles Leclerc", "S76561198000000003", "ks_lamborghini_huracan_gt3", "green_3"),
    ("Lando Norris", "S76561198000000004", "ks_mclaren_650s_gt3", "orange_4"),
    ("Carlos Sainz", "S76561198000000005", "ks_mercedes_amg_gt3", "silver_5"),
    ("George Russell", "S76561198000000006", "ks_audi_r8_lms_2016", "blue_6"),
    ("Fernando Alonso", "S76561198000000007", "ks_bmw_m6_gt3", "blue_7"),
    ("Sebastian Vettel", "S76561198000000008", "ks_nissan_gtr_gt3", "white_8"),
    ("Kimi Raikkonen", "S76561198000000009", "ks_corvette_c7r", "yellow_9"),
    ("Daniel Ricciardo", "S76561198000000010", "ks_ford_gt40", "red_10"),
];
var drivers = allDrivers[..count];

// Base lap times in ms for Spa (~2:17–2:22), one per driver slot
int[] baseLapTimes =
[
    40_200,
    41_000,
    42_000,
    43_000,
    44_000,
    45_000,
    46_000,
    47_000,
    48_000,
    49_500,
];

// ── NewSession ──────────────────────────────────────────────────────────────
Console.WriteLine(">> Sending NewSession (Practice @ Spa)...");
await Send(
    writer
        .WriteByte(50) // NewSession
        .WriteByte(4) // protocol version
        .WriteByte(0) // session index
        .WriteByte(0) // current session index
        .WriteByte(1) // session count
        .WriteStringW("AC Timing Dev Server")
        .WriteString("csp/7/00/:/asdf/simulator") // sending track identifieres like content manager will do
        .WriteString("") // track config
        .WriteString("Practice")
        .WriteByte(1) // type: Practice
        .WriteUInt16(1800) // session time (s)
        .WriteUInt16(0) // laps (0 = timed)
        .WriteUInt16(60) // wait time
        .WriteByte(22) // ambient temp
        .WriteByte(28) // road temp
        .WriteString("3_clear") // weather
        .WriteInt32(0)
); // elapsed ms
await Task.Delay(500);

// ── Connect drivers ──────────────────────────────────────────────────────────
for (var i = 0; i < count; i++)
{
    var (name, guid, car, skin) = drivers[i];
    Console.WriteLine($">> Driver connected: {name, -20} ({car})");
    await Send(
        writer
            .WriteByte(51) // NewConnection
            .WriteStringW(name)
            .WriteStringW(guid)
            .WriteByte((byte)i) // car id
            .WriteString(car)
            .WriteString(skin)
    );
    await Task.Delay(100);
}

Console.WriteLine();
Console.WriteLine("Simulating...");
Console.WriteLine();

// ── Driver state ─────────────────────────────────────────────────────────────
var splinePos = new float[count];
var lapCounts = new int[count];
var bestLapMs = new int[count];
var lapVariance = new int[count];
var lapStartTick = new long[count];
var gears = new int[count];
var rpms = new float[count];

// ── Pit lane geometry & per-driver pit state ─────────────────────────────────
// Image coordinate system (map.ini: SCALE_FACTOR=2, Z_OFFSET=500):
//   py = (worldZ + 500) / 2  →  worldZ = py × 2 − 500
//   py=180 (top of pit)  → worldZ = −140
//   py=320 (bottom of pit) → worldZ = +140
// Cars enter from the TOP of the image and drive DOWN (increasing worldZ).
const float pitX = 500f;
const float pitEntryZ = -140f; // py=180, top of image
const float pitExitZ = 140f; // py=320, bottom of image
const float pitLaneLength = 280f; // |140 − (−140)| = 280 m
const float pitMaxSpeedMs = 80f / 3.6f; // 80 km/h ≈ 22.2 m/s
const float pitStopDurationMs = 5_000f; // 5 seconds stopped in pit box

// Pit lane spline range on the main track
// On the ellipse: spline 0.0 = rightmost point (600,0), going counterclockwise
// worldZ=−140 → sin(angle)=−0.4 → angle≈5.87 rad → spline≈0.935 (upper-right, entry)
// worldZ=+140 → sin(angle)=+0.4 → angle≈0.41 rad → spline≈0.065 (lower-right, exit)
const float pitEntrySpline = 0.935f; // where car leaves the racing line
const float pitExitSpline = 0.065f; // where car rejoins the racing line

// Per-driver pit state
var pitPhase = new int[count]; // 0=racing, 1=entering, 2=stopped, 3=exiting
var pitProgress = new float[count]; // 0..1 along the pit lane (entry→exit)
var pitStopTimer = new float[count]; // ms remaining at pit box

// Spread drivers evenly around the track so the UI is instantly crowded.
// Initialise lapStartTick so the first lap time is realistic (as if each
// driver started from the beginning of the lap at the appropriate offset).
for (var i = 0; i < count; i++)
{
    splinePos[i] = i / (float)count;
    lapVariance[i] = rng.Next(-2_000, 2_000);
    lapStartTick[i] = -(long)(splinePos[i] * baseLapTimes[i] / tickMs);
    gears[i] = 3;
    rpms[i] = 5_000f;
}

// Rough elliptical track dimensions (metres, matches Spa-ish scale)
const float Rx = 600f;
const float Rz = 350f;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var tick = 0L;
try
{
    while (!cts.Token.IsCancellationRequested)
    {
        tick++;

        // ── Advance every driver, detect lap completions ──────────────────
        for (var i = 0; i < count; i++)
        {
            // Don't advance spline while in pit lane
            if (pitPhase[i] > 0)
                continue;

            var lapMs = baseLapTimes[i] + lapVariance[i];
            splinePos[i] += (float)tickMs / lapMs;

            if (splinePos[i] < 1.0f)
                continue;

            splinePos[i] -= 1.0f;

            var elapsed = (int)((tick - lapStartTick[i]) * tickMs);
            lapStartTick[i] = tick;
            lapCounts[i]++;
            lapVariance[i] = rng.Next(-2_000, 2_000);

            var isBest = bestLapMs[i] == 0 || elapsed < bestLapMs[i];
            if (isBest)
                bestLapMs[i] = elapsed;

            var cuts = rng.Next(10) == 0 ? (byte)rng.Next(1, 3) : (byte)0;

            Console.WriteLine(
                $"  Lap:  {drivers[i].Name, -20} {FormatTime(elapsed)}{(isBest ? " ** BEST **" : "")}{(cuts > 0 ? $"  [{cuts} cut(s)]" : "")}"
            );

            // LapCompleted (73)
            var pkt = writer
                .WriteByte(73)
                .WriteByte((byte)i)
                .WriteUInt32((uint)elapsed)
                .WriteByte(cuts)
                .WriteByte((byte)count);

            for (var j = 0; j < count; j++)
                pkt.WriteByte((byte)j)
                    .WriteUInt32((uint)bestLapMs[j])
                    .WriteUInt16((ushort)lapCounts[j]);

            pkt.WriteByte(98); // grip level
            await Send(pkt);
        }

        // ── CarUpdate for every driver (53) ───────────────────────────────
        for (var i = 0; i < count; i++)
        {
            float posX,
                posZ;
            float velX = 0f,
                velZ = 0f;

            // ── Check for pit entry trigger ───────────────────────────────
            // ~8% chance when crossing the pit entry zone, not already pitting
            if (pitPhase[i] == 0 && splinePos[i] is > 0.92f and < 0.95f && rng.NextDouble() < 0.08)
            {
                pitPhase[i] = 1;
                pitProgress[i] = 0f;
                Console.WriteLine($"  Pit:  {drivers[i].Name, -20} entering pit lane");
            }

            if (pitPhase[i] > 0)
            {
                // ── Pit lane simulation ───────────────────────────────────
                // Car moves along the pit lane at max 80 km/h
                var pitDistancePerTick = pitMaxSpeedMs * (tickMs / 1000f);
                var progressPerTick = pitDistancePerTick / pitLaneLength;

                switch (pitPhase[i])
                {
                    case 1: // driving through pit lane toward pit box (midpoint)
                        pitProgress[i] += progressPerTick;
                        if (pitProgress[i] >= 0.5f)
                        {
                            pitProgress[i] = 0.5f;
                            pitPhase[i] = 2;
                            pitStopTimer[i] = pitStopDurationMs;
                            Console.WriteLine($"  Pit:  {drivers[i].Name, -20} stopped in pit box");
                        }
                        break;

                    case 2: // stopped at pit box
                        pitStopTimer[i] -= tickMs;
                        if (pitStopTimer[i] <= 0)
                        {
                            pitPhase[i] = 3;
                            Console.WriteLine($"  Pit:  {drivers[i].Name, -20} leaving pit box");
                        }
                        break;

                    case 3: // driving out of pit lane
                        pitProgress[i] += progressPerTick;
                        if (pitProgress[i] >= 1.0f)
                        {
                            pitPhase[i] = 0;
                            pitProgress[i] = 1.0f; // stay at exit position this tick; reset next tick
                            splinePos[i] = pitExitSpline;
                            Console.WriteLine($"  Pit:  {drivers[i].Name, -20} back on track");
                        }
                        break;
                }

                // Interpolate world position along the pit lane
                var wp = Math.Clamp(pitProgress[i], 0f, 1f);
                posX = pitX;
                posZ = pitEntryZ + wp * (pitExitZ - pitEntryZ);

                // Velocity: moving in +Z direction at pit speed (0 when stopped)
                if (pitPhase[i] != 2)
                {
                    velX = 0f;
                    velZ = +pitMaxSpeedMs; // moving from -Z toward +Z (top to bottom on screen)
                }

                // Freeze spline while in pits (don't advance on the main track)
            }
            else
            {
                // ── Normal track position (ellipse) ───────────────────────
                var angle = splinePos[i] * 2f * MathF.PI;
                var lapMs = baseLapTimes[i] + lapVariance[i];
                var throttle = MathF.Abs(MathF.Sin(angle * 2f));

                const float TopSpeedMs = 250f / 3.6f;
                var speed = TopSpeedMs * (0.35f + 0.65f * throttle);

                posX = Rx * MathF.Cos(angle);
                posZ = Rz * MathF.Sin(angle);
                velX = -speed * MathF.Sin(angle);
                velZ = speed * MathF.Cos(angle);

                // RPM dynamics
                const float RpmAccel = 650f;
                const float RpmBrake = 950f;
                const float Redline = 7_800f;
                const float ShiftDownRpm = 2_800f;
                const float ShiftUpRpm = 7_500f;

                var rpmDelta =
                    throttle >= 0.45f ? RpmAccel * throttle : -RpmBrake * (1f - throttle);

                rpms[i] = Math.Clamp(rpms[i] + rpmDelta, 1_200f, Redline);

                if (rpms[i] >= ShiftUpRpm && gears[i] < 6)
                {
                    gears[i]++;
                    rpms[i] = 4_200f;
                }
                else if (rpms[i] <= ShiftDownRpm && gears[i] > 1)
                {
                    gears[i]--;
                    rpms[i] = 6_000f;
                }
            }

            var gear = (byte)(pitPhase[i] > 0 ? 1 : gears[i]); // 1st gear in pits
            var rpm = (ushort)(
                pitPhase[i] == 2
                    ? 800 // idle when stopped
                    : Math.Clamp(rpms[i] + rng.Next(-150, 150), 1_000, 8_000)
            );

            await Send(
                writer
                    .WriteByte(53)
                    .WriteByte((byte)i)
                    .WriteVector3(posX, 0f, posZ)
                    .WriteVector3(velX, 0f, velZ)
                    .WriteByte(gear)
                    .WriteUInt16(rpm)
                    .WriteFloat(splinePos[i])
            );
        }

        // ── Random collision events (~1 every ~17 s at 250 ms tick) ──────
        // Probability: 0.015 per tick × (1000/250 ticks/s) = 0.06/s → ~1 per 17 s
        if (rng.NextDouble() < 0.015)
        {
            var carId = rng.Next(count);
            var impact = 10f + rng.NextSingle() * 140f; // 10–150 km/h

            if (count > 1 && rng.NextDouble() < 0.5)
            {
                // Car-vs-car: pick a different car
                var otherId = (carId + 1 + rng.Next(count - 1)) % count;
                Console.WriteLine(
                    $"  Crash: {drivers[carId].Name, -20} → {drivers[otherId].Name, -20} @ {impact, 5:F0} km/h"
                );
                await Send(
                    writer
                        .WriteByte(130) // ClientEvent
                        .WriteByte(10) // CollisionWithCar
                        .WriteByte((byte)carId)
                        .WriteByte((byte)otherId)
                        .WriteFloat(impact)
                );
            }
            else
            {
                // Car-vs-environment
                Console.WriteLine(
                    $"  Crash: {drivers[carId].Name, -20} → environment            @ {impact, 5:F0} km/h"
                );
                await Send(
                    writer
                        .WriteByte(130) // ClientEvent
                        .WriteByte(11) // CollisionWithEnv
                        .WriteByte((byte)carId)
                        .WriteFloat(impact)
                );
            }
        }

        await Task.Delay(tickMs, cts.Token);
    }
}
catch (OperationCanceledException) { }

// ── End session ───────────────────────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine(">> Ending session...");
await Send(writer.WriteByte(55));
Console.WriteLine("Simulator stopped.");

// ── Helpers ───────────────────────────────────────────────────────────────────
async Task Send(AcPacketWriter w)
{
    var data = w.ToArray();
    await udp.SendAsync(data, endpoint);
    writer.Reset();
}

static string FormatTime(int ms)
{
    var ts = TimeSpan.FromMilliseconds(ms);
    return $"{ts.Minutes}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
}
