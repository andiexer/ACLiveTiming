using System.Text.Json.Serialization;

namespace Devlabs.AcTiming.Domain.LiveTiming;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(LiveSessionInfo), "session-info")]
[JsonDerivedType(typeof(LiveSessionEnded), "session-ended")]
[JsonDerivedType(typeof(LiveDriverEntry), "driver-entry")]
[JsonDerivedType(typeof(DriverTelemetry), "telemetry")]
[JsonDerivedType(typeof(LapCompletedEvent), "lap-completed")]
[JsonDerivedType(typeof(CollisionEvent), "collision")]
[JsonDerivedType(typeof(DriverDisconnected), "driver-disconnected")]
[JsonDerivedType(typeof(DriverConnected), "driver-connected")]
public abstract record SimEvent;
