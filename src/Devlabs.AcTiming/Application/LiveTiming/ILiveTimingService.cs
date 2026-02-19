using Devlabs.AcTiming.Domain.LiveTiming;

namespace Devlabs.AcTiming.Application.LiveTiming;

public interface ILiveTimingService
{
    LiveSessionInfo? GetCurrentSession();
    LiveDriverEntry? GetDriver(int carId);
    IReadOnlyList<LiveDriverEntry> GetLeaderboard();

    void UpdateSession(LiveSessionInfo session);
    void UpdateDriver(LiveDriverEntry driver);
    void UpdateDriverTelemetry(DriverTelemetry telemetry);
    void RemoveDriver(int carId);
    void ClearSession();
}
