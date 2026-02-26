using Devlabs.AcTiming.Application.EventRouting.Pipeline.Abstractions;
using Devlabs.AcTiming.Application.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Devlabs.AcTiming.Application.EventRouting.Pipeline.Enrichers.Pit;

public sealed class PitStatusEnricher(
    IServiceScopeFactory scopeFactory,
    IPitLaneProvider pitLaneProvider,
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
                // Async: try DB config first, fall back to file-based spline
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
            // Create a short-lived scope to resolve the scoped repository
            await using var scope = scopeFactory.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetRequiredService<ITrackConfigRepository>();
            var config = await repo.FindByTrackAsync(s.TrackName, s.TrackConfig, ct);

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
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to load TrackConfig from DB for {Track}, falling back to pit_lane.ai",
                s.TrackName
            );
        }

        // Fallback: legacy pit_lane.ai spline
        var spline = pitLaneProvider.LoadPoints(s.TrackName, s.TrackConfig);
        tracker.LoadSpline(spline);

        if (spline is { Length: > 0 })
            logger.LogInformation(
                "Pit detection: using spline fallback ({Count} points) for {Track}",
                spline.Length,
                s.TrackName
            );
        else
            logger.LogInformation(
                "Pit detection: no data available for {Track}, pit detection disabled",
                s.TrackName
            );

        return [];
    }
}
