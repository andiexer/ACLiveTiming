namespace Devlabs.AcTiming.Domain.LiveTiming;

public record SessionEvent(DateTime OccurredAtUtc, EventKind EventKind, SimEvent Event);

public enum EventKind
{
    SessionStart,
    SessionEnd,
    Join,
    Disconnect,
    Collision,
}
