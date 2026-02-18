using System.Net;
using System.Net.Sockets;
using Devlabs.AcTiming.Simulator;

var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 9996;
var driverCount = args.Length > 1 && int.TryParse(args[1], out var d) ? d : 4;
var lapIntervalMs = args.Length > 2 && int.TryParse(args[2], out var l) ? l : 5000;

Console.WriteLine($"AC Timing Simulator - Sending to UDP port {port}");
Console.WriteLine($"Drivers: {driverCount}, Lap interval: {lapIntervalMs}ms");
Console.WriteLine("Press Ctrl+C to stop.");
Console.WriteLine();

using var udpClient = new UdpClient();
var endpoint = new IPEndPoint(IPAddress.Loopback, port);
var writer = new AcPacketWriter();
var random = new Random(42);

var drivers = new[]
{
    ("Max Verstappen", "S76561198000000001", "ks_ferrari_488_gt3", "red_1"),
    ("Lewis Hamilton", "S76561198000000002", "ks_porsche_911_gt3_r", "white_2"),
    ("Charles Leclerc", "S76561198000000003", "ks_lamborghini_huracan_gt3", "green_3"),
    ("Lando Norris", "S76561198000000004", "ks_mclaren_720s_gt3", "orange_4"),
    ("Carlos Sainz", "S76561198000000005", "ks_mercedes_amg_gt3", "silver_5"),
    ("George Russell", "S76561198000000006", "ks_audi_r8_lms_2016", "blue_6"),
};

// Send session info (NewSession = 50)
Console.WriteLine(">> Sending NewSession (Practice @ Spa)...");
var sessionPacket = writer
    .WriteByte(50)  // NewSession
    .WriteByte(4)   // protocol version
    .WriteByte(0)   // session index
    .WriteByte(0)   // current session index
    .WriteByte(1)   // session count
    .WriteStringW("AC Timing Dev Server")
    .WriteString("spa")
    .WriteString("")          // track config
    .WriteString("Practice")
    .WriteByte(1)             // type: practice
    .WriteUInt16(1800)        // time (uint16)
    .WriteUInt16(0)           // laps (uint16, 0 = unlimited)
    .WriteUInt16(60)          // wait time (uint16)
    .WriteByte(22)            // ambient temp
    .WriteByte(28)            // road temp
    .WriteString("3_clear")   // weather graphics
    .WriteInt32(0)            // elapsed ms
    .ToArray();

await udpClient.SendAsync(sessionPacket, endpoint);
writer.Reset();
await Task.Delay(500);

// Connect drivers (NewConnection = 51)
for (var i = 0; i < Math.Min(driverCount, drivers.Length); i++)
{
    var (name, guid, car, skin) = drivers[i];
    Console.WriteLine($">> Driver connected: {name} ({car})");

    var connPacket = writer
        .WriteByte(51)        // NewConnection
        .WriteStringW(name)
        .WriteStringW(guid)
        .WriteByte((byte)i)   // car id
        .WriteString(car)
        .WriteString(skin)
        .ToArray();

    await udpClient.SendAsync(connPacket, endpoint);
    writer.Reset();
    await Task.Delay(200);
}

Console.WriteLine();
Console.WriteLine("Simulating laps...");

var baseLapTimes = new[] { 137_500, 138_200, 138_800, 139_100, 139_500, 140_200 };
var bestLapTimes = new int[driverCount]; // 0 = no lap yet
var lapCounters = new int[driverCount];

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

try
{
    while (!cts.Token.IsCancellationRequested)
    {
        // Random driver completes a lap
        var carId = random.Next(Math.Min(driverCount, drivers.Length));
        lapCounters[carId]++;

        // Add some variance: +/- 2 seconds
        var lapTime = baseLapTimes[carId] + random.Next(-2000, 2000);
        var cuts = random.NextDouble() < 0.1 ? random.Next(1, 3) : 0;

        // Track best lap per driver
        if (bestLapTimes[carId] == 0 || lapTime < bestLapTimes[carId])
        {
            bestLapTimes[carId] = lapTime;
            Console.WriteLine($"  Lap: {drivers[carId].Item1} - {FormatLapTime(lapTime)} ** BEST ** (cuts: {cuts})");
        }
        else
        {
            Console.WriteLine($"  Lap: {drivers[carId].Item1} - {FormatLapTime(lapTime)} (cuts: {cuts})");
        }

        // LapCompleted = 73
        var lapPacket = writer
            .WriteByte(73)
            .WriteByte((byte)carId)
            .WriteUInt32((uint)lapTime)   // uint32
            .WriteByte((byte)cuts)
            .WriteByte((byte)Math.Min(driverCount, drivers.Length));

        // Leaderboard entries - use actual best laps (0 if no lap yet)
        for (var i = 0; i < Math.Min(driverCount, drivers.Length); i++)
        {
            lapPacket
                .WriteByte((byte)i)                            // car id (byte)
                .WriteUInt32((uint)bestLapTimes[i])            // best time (uint32)
                .WriteByte((byte)lapCounters[i]);              // laps (byte)
        }

        lapPacket.WriteByte(98); // grip level (byte, ~98%)

        await udpClient.SendAsync(lapPacket.ToArray(), endpoint);
        writer.Reset();

        // Send car position updates between laps (CarUpdate = 53)
        for (var tick = 0; tick < 5; tick++)
        {
            for (var i = 0; i < Math.Min(driverCount, drivers.Length); i++)
            {
                var pos = (float)((tick * 0.2 + i * 0.15) % 1.0);
                var updatePacket = writer
                    .WriteByte(53)
                    .WriteByte((byte)i)
                    .WriteVector3(random.NextSingle() * 100, 0, random.NextSingle() * 100)
                    .WriteVector3(random.NextSingle() * 50, 0, random.NextSingle() * 50)
                    .WriteByte((byte)random.Next(1, 7))       // gear (byte)
                    .WriteUInt16((ushort)random.Next(3000, 8000)) // rpm (uint16)
                    .WriteFloat(pos)
                    .ToArray();

                await udpClient.SendAsync(updatePacket, endpoint);
                writer.Reset();
            }

            await Task.Delay(lapIntervalMs / 5, cts.Token);
        }
    }
}
catch (OperationCanceledException)
{
    // Expected
}

// End session = 55
Console.WriteLine();
Console.WriteLine(">> Ending session...");
var endPacket = writer.WriteByte(55).ToArray();
await udpClient.SendAsync(endPacket, endpoint);

Console.WriteLine("Simulator stopped.");

static string FormatLapTime(int ms)
{
    var ts = TimeSpan.FromMilliseconds(ms);
    return $"{ts.Minutes}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
}
