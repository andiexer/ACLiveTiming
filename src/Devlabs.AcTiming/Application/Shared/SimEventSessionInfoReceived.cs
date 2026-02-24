using Devlabs.AcTiming.Domain.Shared;

namespace Devlabs.AcTiming.Application.Shared;

public record SimEventSessionInfoReceived(
    string ServerName,
    string TrackName,
    string TrackConfig,
    SessionType Type,
    int Time,
    int Laps,
    int ElapsedMs,
    float AmbientTemp,
    float RoadTemp
) : SimEvent;
