using Devlabs.AcTiming.Domain.Shared;

namespace Devlabs.AcTiming.Domain.LiveTiming;

public sealed record SessionInfo(
    string ServerName,
    string TrackName,
    string? TrackConfig,
    SessionType SessionType,
    int TimeLimitMinutes,
    int LapLimit,
    int ElapsedMs,
    float AmbientTemp,
    float RoadTemp
);
