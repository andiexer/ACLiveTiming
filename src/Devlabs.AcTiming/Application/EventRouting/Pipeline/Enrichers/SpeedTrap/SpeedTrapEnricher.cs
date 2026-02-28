using Devlabs.AcTiming.Application.Abstractions;
using Devlabs.AcTiming.Application.EventRouting.Pipeline.Abstractions;
using Devlabs.AcTiming.Application.Shared;
using Devlabs.AcTiming.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Devlabs.AcTiming.Application.EventRouting.Pipeline.Enrichers.SpeedTrap;

public sealed class SpeedTrapEnricher(
    ILogger<SpeedTrapEnricher> logger,
    SpeedTrapTracker tracker,
    IServiceScopeFactory serviceScopeFactory
) : ISimEventEnricher
{
    public EnricherPhase Phase { get; } = EnricherPhase.Pre;

    public async ValueTask<IReadOnlyList<SimEvent>> EnrichAsync(SimEvent ev, CancellationToken ct)
    {
        switch (ev)
        {
            case SimEventSessionInfoReceived s:
                await HandleSessionInfoAsync(s, ct);
                break;
            case SimEventTelemetryUpdated t:
                return HandleTelemetryUpdated(t);
        }

        return [];
    }

    private IReadOnlyList<SimEvent> HandleTelemetryUpdated(SimEventTelemetryUpdated ev)
    {
        var trapFired = tracker.Process(ev.CarId, ev.WorldX, ev.WorldZ, ev.SpeedKmh);

        if (trapFired is null)
        {
            return [];
        }

        logger.LogInformation(
            "Car {CarId} fired speed trap {TrapName} at speed {SpeedKmh} km/h",
            ev.CarId,
            trapFired.Value.Trap.Name,
            ev.SpeedKmh
        );

        return
        [
            new SimEventSpeedTrapFired(
                ev.CarId,
                trapFired.Value.Trap.Id,
                trapFired.Value.Trap.Name,
                ev.SpeedKmh
            ),
        ];
    }

    private async Task HandleSessionInfoAsync(SimEventSessionInfoReceived ev, CancellationToken ct)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var timingDb = scope.ServiceProvider.GetRequiredService<ITimingDb>();
        var trackConfig = await timingDb
            .AsNoTracking<TrackConfig>()
            .Include(x => x.Track)
            .FirstOrDefaultAsync(
                c => c.Track.Name == ev.TrackName && c.Track.Config == ev.TrackConfig,
                ct
            );

        if (trackConfig is null)
        {
            logger.LogInformation(
                "Track config not found for track {TrackName} with config {TrackConfig}",
                ev.TrackName,
                ev.TrackConfig
            );
            return;
        }

        tracker.LoadTraps(trackConfig.SpeedTraps);
        foreach (var trap in trackConfig.SpeedTraps)
        {
            logger.LogInformation(
                "loaded speed trap {TrapName} at position {PositionA} / {PositionB}",
                trap.Name,
                trap.Point1,
                trap.Point2
            );
        }
    }
}
