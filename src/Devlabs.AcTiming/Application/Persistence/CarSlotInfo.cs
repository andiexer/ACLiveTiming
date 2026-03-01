namespace Devlabs.AcTiming.Application.Persistence;

/// <summary>
/// Tracks which driver and car are currently occupying an AC car slot (carId) in the active session.
/// Updated on <c>SimEventDriverConnected</c> and <c>SimEventCarInfoReceived</c>.
/// </summary>
internal sealed record CarSlotInfo(
    string DriverGuid,
    string DriverName,
    string? Team,
    string CarModel,
    string? CarSkin
);
