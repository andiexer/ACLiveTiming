namespace Devlabs.AcTiming.Domain.Shared;

public class Car : Entity
{
    public required string Model { get; set; }
    public string? Skin { get; set; }

    public ICollection<Lap> Laps { get; set; } = [];
}
