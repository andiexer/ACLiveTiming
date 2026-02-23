namespace Devlabs.AcTiming.Infrastructure.AcServer;

/// <summary>
/// Assetto Corsa dedicated server UDP protocol definitions.
/// Protocol is binary, little-endian, with length-prefixed UTF-32 strings.
/// Default plugin port: 9996.
/// Reference: https://github.com/mathiasuk/ac-pserver
/// </summary>
public static class AcProtocol
{
    public const int DefaultPort = 9996;

    // Server → Plugin packet types
    public const byte NewSession = 50;
    public const byte NewConnection = 51;
    public const byte ConnectionClosed = 52;
    public const byte CarUpdate = 53;
    public const byte CarInfo = 54;
    public const byte EndSession = 55;
    public const byte Version = 56;
    public const byte Chat = 57;
    public const byte ClientLoaded = 58;
    public const byte SessionInfo = 59;
    public const byte Error = 60;
    public const byte LapCompleted = 73;
    public const byte ClientEvent = 130;

    /// <summary>
    /// Sector / split time — requires AC Server Manager (reads server log and re-broadcasts).
    /// Packet: type(1) + carId(1) + splitIndex(1, 0-based) + splitTimeMs(4) [+ cuts(1) — unconfirmed]
    /// </summary>
    public const byte LapSplit = 150;

    // Plugin → Server packet types
    public const byte RealtimePosInterval = 200;
    public const byte GetCarInfo = 201;
    public const byte SendChat = 202;
    public const byte BroadcastChat = 203;
    public const byte GetSessionInfo = 204;
    public const byte SetSessionInfo = 205;
    public const byte KickUser = 206;
    public const byte NextSession = 207;
    public const byte RestartSession = 208;
    public const byte AdminCommand = 209;

    // Client event sub-types
    public const byte CollisionWithCar = 10;
    public const byte CollisionWithEnv = 11;
}

public readonly record struct AcVector3(float X, float Y, float Z);

public record AcCarUpdate
{
    public int CarId { get; init; }
    public AcVector3 Position { get; init; }
    public AcVector3 Velocity { get; init; }
    public int Gear { get; init; }
    public int EngineRpm { get; init; }
    public float NormalizedSplinePos { get; init; }
}

public record AcNewConnection
{
    public required string DriverName { get; init; }
    public required string DriverGuid { get; init; }
    public int CarId { get; init; }
    public required string CarModel { get; init; }
    public required string CarSkin { get; init; }
}

public record AcConnectionClosed
{
    public required string DriverName { get; init; }
    public required string DriverGuid { get; init; }
    public int CarId { get; init; }
    public required string CarModel { get; init; }
    public required string CarSkin { get; init; }
}

public record AcSessionInfo
{
    public int ProtocolVersion { get; init; }
    public int SessionIndex { get; init; }
    public int CurrentSessionIndex { get; init; }
    public int SessionCount { get; init; }
    public required string ServerName { get; init; }
    public required string Track { get; init; }
    public required string TrackConfig { get; init; }
    public required string Name { get; init; }
    public int Type { get; init; } // 1=Practice, 2=Qualifying, 3=Race
    public int Time { get; init; } // uint16 from protocol
    public int Laps { get; init; } // uint16 from protocol
    public int WaitTime { get; init; } // uint16 from protocol
    public int AmbientTemp { get; init; }
    public int RoadTemp { get; init; }
    public required string WeatherGraphics { get; init; }
    public int ElapsedMs { get; init; }
}

public record AcClientEvent
{
    public int EventType { get; init; }
    public int CarId { get; init; }
    public int? OtherCarId { get; init; }
    public float ImpactSpeed { get; init; }
}

public record AcCarInfo
{
    public int CarId { get; init; }
    public bool IsConnected { get; init; }
    public required string CarModel { get; init; }
    public required string CarSkin { get; init; }
    public required string DriverName { get; init; }
    public required string DriverTeam { get; init; }
    public required string DriverGuid { get; init; }
}

public record AcLapCompleted
{
    public int CarId { get; init; }
    public int LapTimeMs { get; init; }
    public int Cuts { get; init; }
    public int CarsCount { get; init; }
    public int GripLevel { get; init; } // byte from protocol
    public List<AcLeaderboardEntry> Leaderboard { get; init; } = [];
}

public record AcLeaderboardEntry
{
    public int CarId { get; init; }
    public int LapTimeMs { get; init; }
    public int Laps { get; init; } // byte from protocol
}
