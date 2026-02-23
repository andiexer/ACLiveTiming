namespace Devlabs.AcTiming.Infrastructure.AcServer;

public static class AcPacketParser
{
    public static AcSessionInfo ParseSessionInfo(ReadOnlySpan<byte> data)
    {
        var reader = new AcPacketReader(data);
        _ = reader.ReadByte(); // packet type

        return new AcSessionInfo
        {
            ProtocolVersion = reader.ReadByte(),
            SessionIndex = reader.ReadByte(),
            CurrentSessionIndex = reader.ReadByte(),
            SessionCount = reader.ReadByte(),
            ServerName = reader.ReadStringW(),
            Track = reader.ReadString(),
            TrackConfig = reader.ReadString(),
            Name = reader.ReadString(),
            Type = reader.ReadByte(),
            Time = reader.ReadUInt16(),
            Laps = reader.ReadUInt16(),
            WaitTime = reader.ReadUInt16(),
            AmbientTemp = reader.ReadByte(),
            RoadTemp = reader.ReadByte(),
            WeatherGraphics = reader.ReadString(),
            ElapsedMs = reader.ReadInt32(),
        };
    }

    public static AcNewConnection ParseNewConnection(ReadOnlySpan<byte> data)
    {
        var reader = new AcPacketReader(data);
        _ = reader.ReadByte(); // packet type

        return new AcNewConnection
        {
            DriverName = reader.ReadStringW(),
            DriverGuid = reader.ReadStringW(),
            CarId = reader.ReadByte(),
            CarModel = reader.ReadString(),
            CarSkin = reader.ReadString(),
        };
    }

    public static AcConnectionClosed ParseConnectionClosed(ReadOnlySpan<byte> data)
    {
        var reader = new AcPacketReader(data);
        _ = reader.ReadByte(); // packet type

        return new AcConnectionClosed
        {
            DriverName = reader.ReadStringW(),
            DriverGuid = reader.ReadStringW(),
            CarId = reader.ReadByte(),
            CarModel = reader.ReadString(),
            CarSkin = reader.ReadString(),
        };
    }

    public static AcCarUpdate ParseCarUpdate(ReadOnlySpan<byte> data)
    {
        var reader = new AcPacketReader(data);
        _ = reader.ReadByte(); // packet type

        return new AcCarUpdate
        {
            CarId = reader.ReadByte(),
            Position = reader.ReadVector3(),
            Velocity = reader.ReadVector3(),
            Gear = reader.ReadByte(),
            EngineRpm = reader.ReadUInt16(),
            NormalizedSplinePos = reader.ReadFloat(),
        };
    }

    public static AcClientEvent ParseClientEvent(ReadOnlySpan<byte> data)
    {
        var reader = new AcPacketReader(data);
        _ = reader.ReadByte(); // packet type

        var eventType = reader.ReadByte();
        var carId = reader.ReadByte();
        int? otherCarId = eventType == AcProtocol.CollisionWithCar ? reader.ReadByte() : null;
        var impactSpeed = reader.ReadFloat();
        // world_pos and rel_pos skipped — not needed for incident display

        return new AcClientEvent
        {
            EventType = eventType,
            CarId = carId,
            OtherCarId = otherCarId,
            ImpactSpeed = impactSpeed,
        };
    }

    public static AcCarInfo ParseCarInfo(ReadOnlySpan<byte> data)
    {
        var reader = new AcPacketReader(data);
        _ = reader.ReadByte(); // packet type

        return new AcCarInfo
        {
            CarId = reader.ReadByte(),
            IsConnected = reader.ReadByte() != 0,
            CarModel = reader.ReadStringW(),
            CarSkin = reader.ReadStringW(),
            DriverName = reader.ReadStringW(),
            DriverTeam = reader.ReadStringW(),
            DriverGuid = reader.ReadStringW(),
        };
    }

    public static AcLapCompleted ParseLapCompleted(ReadOnlySpan<byte> data)
    {
        var reader = new AcPacketReader(data);
        _ = reader.ReadByte(); // packet type

        var carId = reader.ReadByte();
        var lapTime = reader.ReadUInt32();
        var cuts = reader.ReadByte();
        var carsCount = reader.ReadByte();

        var leaderboard = new List<AcLeaderboardEntry>(carsCount);
        for (var i = 0; i < carsCount; i++)
        {
            leaderboard.Add(
                new AcLeaderboardEntry
                {
                    CarId = reader.ReadByte(),
                    LapTimeMs = (int)reader.ReadUInt32(),
                    Laps = reader.ReadUInt16(),
                }
            );
        }

        var gripLevel = reader.ReadByte();

        return new AcLapCompleted
        {
            CarId = carId,
            LapTimeMs = (int)lapTime,
            Cuts = cuts,
            CarsCount = carsCount,
            GripLevel = gripLevel,
            Leaderboard = leaderboard,
        };
    }
}
