namespace Devlabs.AcTiming.Domain.Shared;

public class Track : Entity
{
    public required string Name { get; set; }
    public string? Config { get; set; }

    public ICollection<Session> Sessions { get; set; } = [];
    public ICollection<Lap> Laps { get; set; } = [];

    public TrackConfig? TrackConfig { get; set; }
}
