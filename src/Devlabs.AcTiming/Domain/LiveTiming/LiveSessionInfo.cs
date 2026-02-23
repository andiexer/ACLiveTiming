using Devlabs.AcTiming.Domain.Shared;

namespace Devlabs.AcTiming.Domain.LiveTiming;

public record LiveSessionInfo : SimEvent
{
    public required string ServerName { get; set; }
    public required string TrackName { get; set; }
    public string? TrackConfig { get; set; }
    public SessionType SessionType { get; set; }

    public int TimeLimitMinutes { get; set; }
    public int LapLimit { get; set; }
    public int ElapsedMs { get; set; }
    public int AmbientTemp { get; set; }
    public int RoadTemp { get; set; }

    public List<LiveDriverEntry> Drivers { get; set; } = [];

    public List<SessionEvent> SessionEvents { get; set; } = [];
}
