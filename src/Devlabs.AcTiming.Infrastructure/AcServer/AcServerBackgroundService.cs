using Devlabs.AcTiming.Application.LiveTiming;
using Devlabs.AcTiming.Application.Shared;
using Microsoft.Extensions.Hosting;

namespace Devlabs.AcTiming.Infrastructure.AcServer;

/// <summary>
/// Manages the lifecycle of the AC UDP connection.
/// Event handling and business logic live in <see cref="AcServerEventProcessor"/>.
/// </summary>
public class AcServerBackgroundService : BackgroundService
{
    private readonly IAcUdpClient _udpClient;

    public AcServerBackgroundService(
        IAcUdpClient udpClient,
        // Injected to guarantee the processor is constructed (and subscribed) before the socket opens.
        AcServerEventProcessor _)
    {
        _udpClient = udpClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _udpClient.StartAsync(stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }

        await _udpClient.StopAsync();
    }
}
