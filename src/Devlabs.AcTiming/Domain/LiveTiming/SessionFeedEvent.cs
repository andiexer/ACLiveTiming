using System.Text.Json.Serialization;

namespace Devlabs.AcTiming.Domain.LiveTiming;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(DriverJoinedFeed), "driver-joined")]
[JsonDerivedType(typeof(DriverLeftFeed), "driver-left")]
[JsonDerivedType(typeof(LapCompletedFeed), "lap-completed")]
[JsonDerivedType(typeof(CollisionFeed), "collision")]
[JsonDerivedType(typeof(DriverInPitFeed), "driver-in-pit")]
public abstract record SessionFeedEvent(DateTime OccurredAtUtc);

public record DriverJoinedFeed(DateTime OccurredAtUtc, int CarId, string DriverName)
    : SessionFeedEvent(OccurredAtUtc);

public record DriverLeftFeed(DateTime OccurredAtUtc, int CarId, string DriverName)
    : SessionFeedEvent(OccurredAtUtc);

public record LapCompletedFeed(
    DateTime OccurredAtUtc,
    int CarId,
    string DriverName,
    int LapTimeMs,
    bool Valid
) : SessionFeedEvent(OccurredAtUtc);

public record CollisionFeed(
    DateTime OccurredAtUtc,
    int CarId,
    string DriverName,
    int? OtherCarId,
    string? OtherDriverName,
    float ImpactSpeedKmh
) : SessionFeedEvent(OccurredAtUtc);

public record DriverInPitFeed(DateTime OccurredAtUtc, int CarId, string DriverName)
    : SessionFeedEvent(OccurredAtUtc);
