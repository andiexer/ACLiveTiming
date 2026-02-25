using Devlabs.AcTiming.Application.EventRouting.Pipeline.Abstractions;
using Devlabs.AcTiming.Application.Shared;

namespace Devlabs.AcTiming.Application.EventRouting.Pipeline.Enrichers.SectorTiming;

public sealed class SectorTimingEnricher(SectorTimingTracker tracker) : ISimEventEnricher
{
    public EnricherPhase Phase => EnricherPhase.Pre;

    public ValueTask<IReadOnlyList<SimEvent>> EnrichAsync(SimEvent ev, CancellationToken ct)
    {
        switch (ev)
        {
            case SimEventSessionInfoReceived:
                tracker.ResetAll();
                return ValueTask.FromResult<IReadOnlyList<SimEvent>>([]);

            case SimEventSessionEnded:
                tracker.ResetAll();
                return ValueTask.FromResult<IReadOnlyList<SimEvent>>([]);

            case SimEventDriverDisconnected d:
                tracker.ResetCar(d.CarId);
                return ValueTask.FromResult<IReadOnlyList<SimEvent>>([]);

            case SimEventTelemetryUpdated t:
            {
                var crossing = tracker.ProcessUpdate(t.CarId, t.SplinePosition);
                if (crossing is null)
                    return ValueTask.FromResult<IReadOnlyList<SimEvent>>([]);

                return ValueTask.FromResult<IReadOnlyList<SimEvent>>([
                    new SimEventSectorCrossed(
                        t.CarId,
                        crossing.SectorIndex,
                        crossing.SectorTimeMs,
                        crossing.CompletedSectors,
                        false
                    ),
                ]);
            }

            case SimEventLapCompleted l:
            {
                var sectors = tracker.OnLapCompleted(l.CarId, l.LapTimeMs);
                if (sectors is null)
                    return ValueTask.FromResult<IReadOnlyList<SimEvent>>([]);

                return ValueTask.FromResult<IReadOnlyList<SimEvent>>([
                    new SimEventSectorCrossed(l.CarId, 2, sectors[2], sectors, l.Cuts == 0),
                ]);
            }

            default:
                return ValueTask.FromResult<IReadOnlyList<SimEvent>>([]);
        }
    }
}
