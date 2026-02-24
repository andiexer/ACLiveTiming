namespace Devlabs.AcTiming.Application.Shared;

public record SimEventCollisionDetected(
    int CarId,
    CollisionType Type,
    int? OthercarId,
    float ImpactSpeedKmh,
    DateTime OccurredAtUtc
) : SimEvent;

public enum CollisionType
{
    Car = 10,
    Environment = 11,
}
