using Devlabs.AcTiming.Application.Shared;
using Devlabs.AcTiming.Domain.LiveTiming;

namespace Devlabs.AcTiming.Application.LiveTiming;

public interface ILiveTimingService
{
    void StartSession(SimEventSessionInfoReceived ev);
    void ApplyEvent(SimEvent ev);
    void EndSession();

    SessionInfo? GetCurrentSession();
    LiveDriver? GetDriver(int carId);
    IReadOnlyList<LiveDriver> GetLeaderboard();
    IReadOnlyList<SessionFeedEvent> GetFeedEvents();
    IReadOnlyList<BestLapTelemetry> GetBestLaps();
}
