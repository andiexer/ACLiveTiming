namespace Devlabs.AcTiming.Domain.Shared;

public class Session
{
    public int Id { get; set; }
    public required string ServerName { get; set; }
    public SessionType Type { get; set; }

    public int TrackId { get; set; }
    public Track Track { get; set; } = null!;

    public int TimeLimitMinutes { get; set; }
    public int LapLimit { get; set; }
    public int AmbientTemp { get; set; }
    public int RoadTemp { get; set; }

    public DateTime StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }

    public ICollection<Lap> Laps { get; set; } = [];
    public ICollection<Driver> Drivers { get; set; } = [];
}
