using Devlabs.AcTiming.Application.EventRouting.Pipeline.Abstractions;
using Devlabs.AcTiming.Application.Shared;

namespace Devlabs.AcTiming.Application.EventRouting.Pipeline.Enrichers.Pit;

public sealed class PitStatusEnricher(IPitLaneProvider pitLaneProvider, PitStatusTracker tracker)
    : ISimEventEnricher
{
    public EnricherPhase Phase => EnricherPhase.Pre;

    public ValueTask<IReadOnlyList<SimEvent>> EnrichAsync(SimEvent ev, CancellationToken ct)
    {
        switch (ev)
        {
            case SimEventSessionInfoReceived s:
                tracker.ResetAll();
                tracker.LoadSpline(pitLaneProvider.LoadPoints(s.TrackName, s.TrackConfig));
                return ValueTask.FromResult<IReadOnlyList<SimEvent>>([]);

            case SimEventSessionEnded:
                tracker.ResetAll();
                return ValueTask.FromResult<IReadOnlyList<SimEvent>>([]);

            case SimEventDriverDisconnected d:
                tracker.ResetCar(d.CarId);
                return ValueTask.FromResult<IReadOnlyList<SimEvent>>([]);

            case SimEventTelemetryUpdated t:
            {
                var newStatus = tracker.Process(t.CarId, t.WorldX, t.WorldZ);
                if (newStatus is null)
                    return ValueTask.FromResult<IReadOnlyList<SimEvent>>([]);

                return ValueTask.FromResult<IReadOnlyList<SimEvent>>([
                    new SimEventPitStatusChanged(t.CarId, newStatus.Value),
                ]);
            }

            default:
                return ValueTask.FromResult<IReadOnlyList<SimEvent>>([]);
        }
    }
}
