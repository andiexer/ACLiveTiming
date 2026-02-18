namespace Devlabs.AcTiming.Domain.Shared;

public class Track
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Config { get; set; }

    public ICollection<Session> Sessions { get; set; } = [];
    public ICollection<Lap> Laps { get; set; } = [];
}
