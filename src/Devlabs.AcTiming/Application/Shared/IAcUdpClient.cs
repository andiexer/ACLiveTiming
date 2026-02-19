using Devlabs.AcTiming.Domain.LiveTiming;

namespace Devlabs.AcTiming.Application.Shared;


public interface IAcUdpClient : IAsyncDisposable
{
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);

    Task RequestCarInfoAsync(int carId);
    Task RequestSessionInfoAsync();

    event EventHandler<LiveSessionInfo> SessionStarted;
    event EventHandler SessionEnded;
    event EventHandler<LiveDriverEntry> DriverConnected;
    event EventHandler<int> DriverDisconnected;
    event EventHandler<DriverTelemetry> DriverUpdated;
    event EventHandler<LapCompletedEvent> LapCompleted;
    event EventHandler<CollisionEvent> CollisionOccurred;
}
