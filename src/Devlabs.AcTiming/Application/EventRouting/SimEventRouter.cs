using Devlabs.AcTiming.Application.Shared;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Devlabs.AcTiming.Application.EventRouting;

public sealed class SimEventRouter(
    ILogger<SimEventRouter> logger,
    ISimEventSource source,
    RealtimeBus realtimeBus,
    PersistenceBus persistenceBus,
    IPitLaneProvider pitLaneProvider
) : BackgroundService
{
    private readonly SectorTimingTracker _sectorTracker = new();
    private readonly PitStatusTracker _pitTracker = new();

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var ev in source.ReadSimEventsAsync(ct))
            {
                try
                {
                    logger.LogDebug("Routing event: {EventType}", ev.GetType().Name);
                    EnrichAndRoute(ev);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error routing event {EventType}", ev.GetType().Name);
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // normal shutdown — not an error
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SimEventRouter crashed");
        }
        finally
        {
            realtimeBus.Writer.TryComplete();
            persistenceBus.Writer.TryComplete();
        }
    }

    private void EnrichAndRoute(SimEvent ev)
    {
        // Tracker lifecycle management
        switch (ev)
        {
            case SimEventSessionInfoReceived s:
                _sectorTracker.ResetAll();
                _pitTracker.ResetAll();
                _pitTracker.LoadSpline(pitLaneProvider.LoadPoints(s.TrackName, s.TrackConfig));
                break;
            case SimEventSessionEnded:
                _sectorTracker.ResetAll();
                _pitTracker.ResetAll();
                break;
            case SimEventDriverDisconnected d:
                _sectorTracker.ResetCar(d.CarId);
                _pitTracker.ResetCar(d.CarId);
                break;
        }

        // For telemetry: emit sector crossing before the original event
        if (ev is SimEventTelemetryUpdated t)
        {
            var crossing = _sectorTracker.ProcessUpdate(t.CarId, t.SplinePosition);
            if (crossing is not null)
            {
                Publish(
                    new SimEventSectorCrossed(
                        t.CarId,
                        crossing.SectorIndex,
                        crossing.SectorTimeMs,
                        crossing.CompletedSectors,
                        false
                    )
                );
            }

            var newPitStatus = _pitTracker.Process(t.CarId, t.WorldX, t.WorldZ);
            if (newPitStatus is not null)
            {
                Publish(new SimEventPitStatusChanged(t.CarId, newPitStatus.Value));
            }
        }

        // For lap completed: write lap event first, then S3 sector finalization
        if (ev is SimEventLapCompleted l)
        {
            Publish(ev);
            var sectors = _sectorTracker.OnLapCompleted(l.CarId, l.LapTimeMs);
            if (sectors is not null)
            {
                var s3Event = new SimEventSectorCrossed(
                    l.CarId,
                    2,
                    sectors[2],
                    sectors,
                    l.Cuts == 0
                );
                Publish(s3Event);
            }
            return;
        }

        Publish(ev);
    }

    private void Publish(SimEvent ev)
    {
        realtimeBus.Writer.TryWrite(ev);
        persistenceBus.Writer.TryWrite(ev);
    }
}
