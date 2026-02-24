namespace Devlabs.AcTiming.Application.Shared;

public record SimEventPitStatusChanged(int CarId, bool IsInPit) : SimEvent;
