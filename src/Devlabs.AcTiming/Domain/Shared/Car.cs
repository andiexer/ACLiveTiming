namespace Devlabs.AcTiming.Domain.Shared;

public class Car
{
    public int Id { get; set; }
    public required string Model { get; set; }
    public string? Skin { get; set; }

    public ICollection<Lap> Laps { get; set; } = [];
}
