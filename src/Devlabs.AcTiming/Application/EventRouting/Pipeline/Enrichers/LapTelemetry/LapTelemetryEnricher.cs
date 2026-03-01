using Devlabs.AcTiming.Application.EventRouting.Pipeline.Abstractions;
using Devlabs.AcTiming.Application.Shared;

namespace Devlabs.AcTiming.Application.EventRouting.Pipeline.Enrichers.LapTelemetry;

public sealed class LapTelemetryEnricher(LapTelemetryTracker tracker) : ISimEventEnricher
{
    public EnricherPhase Phase => EnricherPhase.Pre;

    public ValueTask<IReadOnlyList<SimEvent>> EnrichAsync(SimEvent @event, CancellationToken ct)
    {
        switch (@event)
        {
            case SimEventSessionInfoReceived:
                tracker.ResetAll();
                return Empty;

            case SimEventDriverDisconnected d:
                tracker.ResetCar(d.CarId);
                return Empty;

            case SimEventTelemetryUpdated t:
                tracker.AppendSample(
                    t.CarId,
                    t.SplinePosition,
                    t.WorldX,
                    t.WorldZ,
                    t.SpeedKmh,
                    t.Gear
                );
                return Empty;

            case SimEventLapCompleted l:
            {
                var (samples, maxSpeed) = tracker.TakeLapSnapshot(l.CarId);
                return ValueTask.FromResult<IReadOnlyList<SimEvent>>([
                    new SimEventLapSnapshotted(l.CarId, samples, maxSpeed),
                ]);
            }

            default:
                return Empty;
        }
    }

    private static readonly ValueTask<IReadOnlyList<SimEvent>> Empty = ValueTask.FromResult<
        IReadOnlyList<SimEvent>
    >([]);
}
