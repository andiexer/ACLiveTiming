using Devlabs.AcTiming.Application.LiveTiming;
using Devlabs.AcTiming.Application.Shared;
using Devlabs.AcTiming.Domain.LiveTiming;
using Microsoft.AspNetCore.SignalR;

namespace Devlabs.AcTiming.Web.Hubs;

/// <summary>
/// Subscribes to AC UDP events and pushes updates to SignalR clients.
/// Registered as a hosted service.
/// </summary>
public class TimingHubNotifier : IHostedService
{
    private readonly IAcUdpClient _udpClient;
    private readonly IHubContext<TimingHub> _hubContext;
    private readonly ILiveTimingService _liveTimingService;

    public TimingHubNotifier(IAcUdpClient udpClient, IHubContext<TimingHub> hubContext, ILiveTimingService liveTimingService)
    {
        _udpClient = udpClient;
        _hubContext = hubContext;
        _liveTimingService = liveTimingService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _udpClient.SessionStarted += OnSessionStarted;
        _udpClient.SessionEnded += OnSessionEnded;
        _udpClient.DriverConnected += OnDriverConnected;
        _udpClient.DriverDisconnected += OnDriverDisconnected;
        _udpClient.DriverUpdated += OnDriverUpdated;
        _udpClient.LapCompleted += OnLapCompleted;
        _udpClient.CollisionOccurred += OnCollisionOccurred;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _udpClient.SessionStarted -= OnSessionStarted;
        _udpClient.SessionEnded -= OnSessionEnded;
        _udpClient.DriverConnected -= OnDriverConnected;
        _udpClient.DriverDisconnected -= OnDriverDisconnected;
        _udpClient.DriverUpdated -= OnDriverUpdated;
        _udpClient.LapCompleted -= OnLapCompleted;
        _udpClient.CollisionOccurred -= OnCollisionOccurred;
        return Task.CompletedTask;
    }

    private async void OnSessionStarted(object? sender, LiveSessionInfo session) =>
        await _hubContext.Clients.All.SendAsync(TimingHubMethods.SessionUpdated, session);

    private async void OnSessionEnded(object? sender, EventArgs e) =>
        await _hubContext.Clients.All.SendAsync(TimingHubMethods.SessionUpdated, (LiveSessionInfo?)null);

    private async void OnDriverConnected(object? sender, LiveDriverEntry driver)
    {
        // Read merged state so the UI gets the full picture
        var merged = _liveTimingService.GetDriver(driver.CarId) ?? driver;
        await _hubContext.Clients.All.SendAsync(TimingHubMethods.DriverUpdated, merged);
    }

    private async void OnDriverDisconnected(object? sender, int carId) =>
        await _hubContext.Clients.All.SendAsync(TimingHubMethods.DriverDisconnected, carId);

    private async void OnDriverUpdated(object? sender, DriverTelemetry telemetry)
    {
        // CarUpdate packets fire very frequently - read merged state from service
        // so the UI always gets a complete, up-to-date entry
        var merged = _liveTimingService.GetDriver(telemetry.CarId);
        if (merged is not null)
            await _hubContext.Clients.All.SendAsync(TimingHubMethods.DriverUpdated, merged);
    }

    private async void OnLapCompleted(object? sender, LapCompletedEvent evt)
    {
        // Push the full updated leaderboard so the UI refreshes positions, lap times, etc.
        var leaderboard = _liveTimingService.GetLeaderboard();
        await _hubContext.Clients.All.SendAsync(TimingHubMethods.LeaderboardUpdated, leaderboard);
    }

    private async void OnCollisionOccurred(object? sender, CollisionEvent evt) =>
        await _hubContext.Clients.All.SendAsync(TimingHubMethods.CollisionOccurred, evt);
}
