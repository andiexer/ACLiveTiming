namespace Devlabs.AcTiming.Application.Shared;

public record SimEventDriverDisconnected(
    int CarId,
    string CarModel,
    string CarSkin,
    string DriverName,
    string DriverGuid
) : SimEvent;
