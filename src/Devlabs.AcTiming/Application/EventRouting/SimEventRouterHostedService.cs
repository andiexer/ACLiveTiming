using Devlabs.AcTiming.Application.Abstractions;
using Devlabs.AcTiming.Application.EventRouting.Pipeline;
using Devlabs.AcTiming.Application.Shared;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Devlabs.AcTiming.Application.EventRouting;

public sealed class SimEventRouterHostedService(
    ILogger<SimEventRouterHostedService> logger,
    ISimEventSource source,
    SimEventPipeline pipeline
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var ev in source.ReadSimEventsAsync(ct))
            {
                await pipeline.RouteAsync(ev, ct);
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
    }
}
