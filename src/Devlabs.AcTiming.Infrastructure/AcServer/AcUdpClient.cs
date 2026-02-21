using System.Net;
using System.Net.Sockets;
using Devlabs.AcTiming.Application.Shared;
using Devlabs.AcTiming.Domain.LiveTiming;
using Devlabs.AcTiming.Domain.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Devlabs.AcTiming.Infrastructure.AcServer;

public class AcUdpClient : IAcUdpClient
{
    private readonly ILogger<AcUdpClient> _logger;
    private readonly AcServerOptions _options;
    private UdpClient? _udpClient;
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;
    private IPEndPoint? _serverEndpoint;

    public event EventHandler<LiveSessionInfo>? SessionStarted;
    public event EventHandler? SessionEnded;
    public event EventHandler<LiveDriverEntry>? DriverConnected;
    public event EventHandler<int>? DriverDisconnected;
    public event EventHandler<DriverTelemetry>? DriverUpdated;
    public event EventHandler<LapCompletedEvent>? LapCompleted;
    public event EventHandler<CollisionEvent>? CollisionOccurred;

    public AcUdpClient(ILogger<AcUdpClient> logger, IOptions<AcServerOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        _udpClient = new UdpClient(_options.UdpPort);

        // Use explicit server endpoint if configured, otherwise auto-detect from first packet
        if (!string.IsNullOrEmpty(_options.ServerHost) && _options.ServerPort > 0)
        {
            _serverEndpoint = new IPEndPoint(IPAddress.Parse(_options.ServerHost), _options.ServerPort);
            _logger.LogInformation("AC server endpoint configured: {Endpoint}", _serverEndpoint);
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _receiveTask = ReceiveLoopAsync(_cts.Token);

        _logger.LogInformation("AC UDP client listening on port {Port}", _options.UdpPort);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
        }

        if (_receiveTask is not null)
        {
            try { await _receiveTask; }
            catch (OperationCanceledException) { }
        }

        _udpClient?.Dispose();
        _udpClient = null;

        _logger.LogInformation("AC UDP client stopped");
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _udpClient is not null)
        {
            try
            {
                var result = await _udpClient.ReceiveAsync(ct);

                if (_serverEndpoint is null)
                {
                    _serverEndpoint = result.RemoteEndPoint;
                    _logger.LogInformation("AC server auto-detected at {Endpoint}", _serverEndpoint);
                }

                await ProcessPacketAsync(result.Buffer);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving UDP packet");
            }
        }
    }

    private async Task SendRealtimePosIntervalAsync()
    {
        if (_udpClient is null || _serverEndpoint is null || _options.RealtimePosIntervalMs <= 0)
            return;

        // RealtimePosInterval: byte(200) + uint16(interval ms)
        var packet = new byte[3];
        packet[0] = AcProtocol.RealtimePosInterval;
        BitConverter.TryWriteBytes(packet.AsSpan(1), (ushort)_options.RealtimePosIntervalMs);

        await _udpClient.SendAsync(packet, _serverEndpoint);
        _logger.LogInformation("Sent RealtimePosInterval ({Interval}ms) to {Endpoint}", _options.RealtimePosIntervalMs, _serverEndpoint);
    }

    public async Task RequestCarInfoAsync(int carId)
    {
        if (_udpClient is null || _serverEndpoint is null) return;
        var packet = new byte[] { AcProtocol.GetCarInfo, (byte)carId };
        await _udpClient.SendAsync(packet, _serverEndpoint);
    }

    public async Task RequestSessionInfoAsync()
    {
        if (_udpClient is null || _serverEndpoint is null) return;
        // GetSessionInfo: byte(204) + int16(sessionIndex), -1 = current session
        var packet = new byte[] { AcProtocol.GetSessionInfo, 0xFF, 0xFF };
        await _udpClient.SendAsync(packet, _serverEndpoint);
    }

    private async Task SendGetCarInfoRangeAsync()
    {
        if (_udpClient is null || _serverEndpoint is null) return;

        // GetCarInfo: byte(201) + byte(carId)
        var packet = new byte[2];
        packet[0] = AcProtocol.GetCarInfo;
        for (var i = 0; i < _options.MaxCarSlots; i++)
        {
            packet[1] = (byte)i;
            await _udpClient.SendAsync(packet, _serverEndpoint);
        }

        _logger.LogInformation("Requested CarInfo for slots 0–{Max}", _options.MaxCarSlots - 1);
    }

    private void HandleCarInfo(byte[] data)
    {
        var info = AcPacketParser.ParseCarInfo(data);
        if (!info.IsConnected) return;

        _logger.LogInformation("CarInfo: Driver={Name} GUID={Guid} CarId={CarId} Car={Model}",
            info.DriverName, info.DriverGuid, info.CarId, info.CarModel);

        var entry = new LiveDriverEntry
        {
            CarId = info.CarId,
            DriverName = info.DriverName,
            DriverGuid = info.DriverGuid,
            Team = info.DriverTeam,
            CarModel = info.CarModel,
            CarSkin = info.CarSkin,
            IsConnected = true
        };
        DriverConnected?.Invoke(this, entry);
    }

    private async Task ProcessPacketAsync(byte[] data)
    {
        if (data.Length == 0) return;

        var packetType = data[0];

        try
        {
            switch (packetType)
            {
                case AcProtocol.NewSession:
                case AcProtocol.SessionInfo:
                    HandleSessionInfo(data);
                    await SendRealtimePosIntervalAsync();
                    await SendGetCarInfoRangeAsync();
                    break;
                case AcProtocol.EndSession:
                    _logger.LogDebug("EndSession received");
                    SessionEnded?.Invoke(this, EventArgs.Empty);
                    break;
                case AcProtocol.NewConnection:
                    HandleNewConnection(data);
                    break;
                case AcProtocol.ConnectionClosed:
                    HandleConnectionClosed(data);
                    break;
                case AcProtocol.Version:
                    _logger.LogDebug("Version packet received (protocol handshake)");
                    break;
                case AcProtocol.CarInfo:
                    HandleCarInfo(data);
                    break;
                case AcProtocol.CarUpdate:
                    HandleCarUpdate(data);
                    break;
                case AcProtocol.LapCompleted:
                    HandleLapCompleted(data);
                    break;
                case AcProtocol.ClientEvent:
                    HandleClientEvent(data);
                    break;
                case AcProtocol.LapSplit:
                    _logger.LogInformation("LapSplit raw ({Len} bytes): {Hex}",
                        data.Length,
                        Convert.ToHexString(data));
                    break;
                default:
                    _logger.LogDebug("Unhandled packet type: {PacketType}", packetType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing packet type {PacketType}", packetType);
        }
    }

    private void HandleSessionInfo(byte[] data)
    {
        var info = AcPacketParser.ParseSessionInfo(data);
        _logger.LogDebug("SessionInfo: Server={Server} Track={Track}/{Config} Session={Name} Type={Type} Time={Time}min Laps={Laps} Elapsed={Elapsed}ms Ambient={Ambient}°C Road={Road}°C",
            info.ServerName, info.Track, info.TrackConfig, info.Name, info.Type, info.Time, info.Laps, info.ElapsedMs, info.AmbientTemp, info.RoadTemp);
        var session = new LiveSessionInfo
        {
            ServerName = info.ServerName,
            TrackName = info.Track,
            TrackConfig = info.TrackConfig,
            SessionType = (SessionType)info.Type,
            TimeLimitMinutes = info.Time,
            LapLimit = info.Laps,
            ElapsedMs = info.ElapsedMs,
            AmbientTemp = info.AmbientTemp,
            RoadTemp = info.RoadTemp
        };
        SessionStarted?.Invoke(this, session);
    }

    private void HandleNewConnection(byte[] data)
    {
        var conn = AcPacketParser.ParseNewConnection(data);
        _logger.LogDebug("NewConnection: Driver={Name} GUID={Guid} CarId={CarId} Car={Model} Skin={Skin}",
            conn.DriverName, conn.DriverGuid, conn.CarId, conn.CarModel, conn.CarSkin);
        var entry = new LiveDriverEntry
        {
            CarId = conn.CarId,
            DriverName = conn.DriverName,
            DriverGuid = conn.DriverGuid,
            CarModel = conn.CarModel,
            CarSkin = conn.CarSkin,
            IsConnected = true
        };
        DriverConnected?.Invoke(this, entry);
    }

    private void HandleConnectionClosed(byte[] data)
    {
        var conn = AcPacketParser.ParseConnectionClosed(data);
        _logger.LogDebug("ConnectionClosed: Driver={Name} GUID={Guid} CarId={CarId} Car={Model}",
            conn.DriverName, conn.DriverGuid, conn.CarId, conn.CarModel);
        DriverDisconnected?.Invoke(this, conn.CarId);
    }

    private void HandleCarUpdate(byte[] data)
    {
        var update = AcPacketParser.ParseCarUpdate(data);
        var v = update.Velocity;
        var speedKmh = MathF.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z) * 3.6f;
        /*_logger.LogDebug("CarUpdate: CarId={CarId} Pos=({X:F1},{Y:F1},{Z:F1}) Gear={Gear} RPM={Rpm} Spline={Spline:F4} Speed={Speed:F1}km/h",
            update.CarId, update.Position.X, update.Position.Y, update.Position.Z, update.Gear, update.EngineRpm, update.NormalizedSplinePos, speedKmh);*/

        var telemetry = new DriverTelemetry
        {
            CarId = update.CarId,
            SplinePosition = update.NormalizedSplinePos,
            WorldX = update.Position.X,
            WorldZ = update.Position.Z,
            SpeedKmh = speedKmh,
            Gear = update.Gear,
            EngineRpm = update.EngineRpm
        };
        DriverUpdated?.Invoke(this, telemetry);
    }

    private void HandleLapCompleted(byte[] data)
    {
        var lapInfo = AcPacketParser.ParseLapCompleted(data);
        _logger.LogDebug("LapCompleted: CarId={CarId} LapTime={LapTime}ms Cuts={Cuts} Grip={Grip} Cars={Cars} Leaderboard=[{Leaderboard}]",
            lapInfo.CarId, lapInfo.LapTimeMs, lapInfo.Cuts, lapInfo.GripLevel, lapInfo.CarsCount,
            string.Join(", ", lapInfo.Leaderboard.Select(e => $"Car{e.CarId}:{e.LapTimeMs}ms/{e.Laps}laps")));
        var evt = new LapCompletedEvent
        {
            CarId = lapInfo.CarId,
            LapTimeMs = lapInfo.LapTimeMs,
            Cuts = lapInfo.Cuts,
            Leaderboard = lapInfo.Leaderboard.Select(e => new LeaderboardEntry
            {
                CarId = e.CarId,
                BestLapTimeMs = e.LapTimeMs,
                TotalLaps = e.Laps
            }).ToList()
        };
        LapCompleted?.Invoke(this, evt);
    }

    private void HandleClientEvent(byte[] data)
    {
        var evt = AcPacketParser.ParseClientEvent(data);
        _logger.LogInformation("Collision: Type={Type} Car={CarId} OtherCar={OtherCarId} Speed={Speed:F1}km/h",
            evt.EventType, evt.CarId, evt.OtherCarId, evt.ImpactSpeed);

        var collision = new CollisionEvent
        {
            CarId = evt.CarId,
            Type = (CollisionType)evt.EventType,
            OtherCarId = evt.OtherCarId,
            ImpactSpeedKmh = evt.ImpactSpeed,
            OccurredAtUtc = DateTime.UtcNow
        };
        CollisionOccurred?.Invoke(this, collision);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        GC.SuppressFinalize(this);
    }
}
