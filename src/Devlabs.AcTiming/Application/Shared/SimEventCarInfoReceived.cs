namespace Devlabs.AcTiming.Application.Shared;

public record SimEventCarInfoReceived(
    int CarId,
    string CarModel,
    string CarSkin,
    string DriverName,
    string DriverGuid
) : SimEvent;
