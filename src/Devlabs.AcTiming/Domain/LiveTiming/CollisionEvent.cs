namespace Devlabs.AcTiming.Domain.LiveTiming;

public record CollisionEvent : SimEvent
{
    public int CarId { get; init; }
    public CollisionType Type { get; init; }
    public int? OtherCarId { get; init; }
    public float ImpactSpeedKmh { get; init; }
    public DateTime OccurredAtUtc { get; init; }
}

public enum CollisionType
{
    Car = 10,
    Environment = 11,
}
