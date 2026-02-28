using Devlabs.AcTiming.Application.Abstractions;
using Devlabs.AcTiming.Application.EventRouting.Pipeline.Abstractions;
using Devlabs.AcTiming.Application.Shared;
using Devlabs.AcTiming.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Devlabs.AcTiming.Application.EventRouting.Pipeline.Enrichers.Pit;

public sealed class PitStatusEnricher(
    IServiceScopeFactory scopeFactory,
    PitStatusTracker tracker,
    ILogger<PitStatusEnricher> logger
) : ISimEventEnricher
{
    public EnricherPhase Phase => EnricherPhase.Pre;

    public ValueTask<IReadOnlyList<SimEvent>> EnrichAsync(SimEvent ev, CancellationToken ct)
    {
        switch (ev)
        {
            case SimEventSessionInfoReceived s:
                return LoadPitDataAndResetAsync(s, ct);

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

    private async ValueTask<IReadOnlyList<SimEvent>> LoadPitDataAndResetAsync(
        SimEventSessionInfoReceived s,
        CancellationToken ct
    )
    {
        tracker.ResetAll();

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var timingDb = scope.ServiceProvider.GetRequiredService<ITimingDb>();
            var config = await timingDb
                .AsNoTracking<TrackConfig>()
                .Include(x => x.Track)
                .FirstOrDefaultAsync(
                    c => c.Track.Name == s.TrackName && c.Track.Config == s.TrackConfig,
                    ct
                );

            if (config?.PitLane is { } pitLane)
            {
                var polygon = pitLane.ToPolygon();
                if (polygon.Count >= 3)
                {
                    tracker.LoadPolygon(polygon);
                    logger.LogInformation(
                        "Pit detection: using DB polygon ({Count} vertices) for {Track}",
                        polygon.Count,
                        s.TrackName
                    );
                    return [];
                }
            }
            else
            {
                logger.LogInformation(
                    "No pit lane definition found in DB for {Track}. no pit lane detection will be available.",
                    s.TrackName
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to load TrackConfig from DB for {Track}. no pit lane detection will be available.",
                s.TrackName
            );
        }

        return [];
    }
}
