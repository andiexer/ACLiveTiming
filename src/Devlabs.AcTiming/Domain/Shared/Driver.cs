namespace Devlabs.AcTiming.Domain.Shared;

public class Driver
{
    public int Id { get; set; }
    public required string Guid { get; set; }
    public required string Name { get; set; }
    public string? Team { get; set; }

    public ICollection<Lap> Laps { get; set; } = [];
    public ICollection<Session> Sessions { get; set; } = [];
}
