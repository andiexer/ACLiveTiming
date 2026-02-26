using Devlabs.AcTiming.Application.Shared;
using Devlabs.AcTiming.Domain.LiveTiming;

namespace Devlabs.AcTiming.Application.LiveTiming;

public class LiveTimingService : ILiveTimingService
{
    private LiveTimingSession? _session;
    private readonly Lock _sessionLock = new();

    public void StartSession(SimEventSessionInfoReceived ev)
    {
        lock (_sessionLock)
            _session = new LiveTimingSession(ev);
    }

    public void ApplyEvent(SimEvent ev) => _session?.Apply(ev);

    public void EndSession()
    {
        lock (_sessionLock)
            _session = null;
    }

    public SessionInfo? GetCurrentSession() => _session?.Info;

    public LiveDriver? GetDriver(int carId) => _session?.GetDriver(carId);

    public IReadOnlyList<LiveDriver> GetLeaderboard() => _session?.GetLeaderboard() ?? [];

    public IReadOnlyList<SessionFeedEvent> GetFeedEvents() => _session?.GetFeedEvents() ?? [];

    public IReadOnlyList<BestLapTelemetry> GetBestLaps() => _session?.GetBestLaps() ?? [];
}
