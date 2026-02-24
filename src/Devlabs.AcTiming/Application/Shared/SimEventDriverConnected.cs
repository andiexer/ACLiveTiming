namespace Devlabs.AcTiming.Application.Shared;

public record SimEventDriverConnected(
    int CarId,
    string CarModel,
    string CarSkin,
    string DriverName,
    string DriverGuid
) : SimEvent;
