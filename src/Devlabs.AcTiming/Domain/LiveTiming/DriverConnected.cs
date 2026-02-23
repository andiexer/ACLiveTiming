namespace Devlabs.AcTiming.Domain.LiveTiming;

public record DriverConnected(int CarId, string DriverName) : SimEvent;
