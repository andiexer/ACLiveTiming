using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using Devlabs.AcTiming.Application.Abstractions;
using Devlabs.AcTiming.Application.Shared;
using Devlabs.AcTiming.Domain.Shared;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CollisionType = Devlabs.AcTiming.Application.Shared.CollisionType;

namespace Devlabs.AcTiming.Infrastructure.AcServer;

public sealed class AcUdpEventSource(
    ILogger<AcUdpEventSource> logger,
    IOptions<AcServerOptions> options
) : BackgroundService, ISimEventSource
{
    private readonly AcServerOptions _options = options.Value;

    private UdpClient? _udpClient;
    private IPEndPoint? _serverEndpoint;

    private readonly Channel<SimEvent> _events = Channel.CreateBounded<SimEvent>(
        new BoundedChannelOptions(50_000)
        {
            SingleWriter = true,
            SingleReader = true,
            FullMode = BoundedChannelFullMode.DropOldest,
        }
    );

    public IAsyncEnumerable<SimEvent> ReadSimEventsAsync(CancellationToken ct = default) =>
        _events.Reader.ReadAllAsync(ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _udpClient = new UdpClient(_options.UdpPort);

            // Use explicit server endpoint if configured, otherwise auto-detect from first packet
            if (!string.IsNullOrEmpty(_options.ServerHost) && _options.ServerPort > 0)
            {
                _serverEndpoint = new IPEndPoint(
                    IPAddress.Parse(_options.ServerHost),
                    _options.ServerPort
                );
                logger.LogInformation("AC server endpoint configured: {Endpoint}", _serverEndpoint);
            }

            logger.LogInformation("AC UDP event source listening on port {Port}", _options.UdpPort);

            while (!stoppingToken.IsCancellationRequested && _udpClient is not null)
            {
                UdpReceiveResult result;
                try
                {
                    result = await _udpClient.ReceiveAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (_serverEndpoint is null)
                {
                    _serverEndpoint = result.RemoteEndPoint;
                    logger.LogInformation("AC server auto-detected at {Endpoint}", _serverEndpoint);
                }

                try
                {
                    ProcessPacket(result.Buffer);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing UDP packet");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AC UDP event source crashed");
            _events.Writer.TryComplete(ex);
            throw;
        }
        finally
        {
            try
            {
                _udpClient?.Dispose();
            }
            catch
            {
                /* ignore */
            }

            _udpClient = null;

            _events.Writer.TryComplete();
            logger.LogInformation("AC UDP event source stopped");
        }
    }

    public async Task RequestCarInfoAsync(int carId)
    {
        if (_udpClient is null || _serverEndpoint is null)
            return;

        // GetCarInfo: byte(201) + byte(carId)
        var packet = new byte[] { AcProtocol.GetCarInfo, (byte)carId };
        await _udpClient.SendAsync(packet, _serverEndpoint);
    }

    public async Task RequestSessionInfoAsync()
    {
        if (_udpClient is null || _serverEndpoint is null)
            return;

        // GetSessionInfo: byte(204) + int16(sessionIndex), -1 = current session
        var packet = new byte[] { AcProtocol.GetSessionInfo, 0xFF, 0xFF };
        await _udpClient.SendAsync(packet, _serverEndpoint);
    }

    // If you still want this convenience for initial sync, keep it internal and call it from Application orchestrator
    public async Task RequestAllCarInfoAsync()
    {
        if (_udpClient is null || _serverEndpoint is null)
            return;

        var packet = new byte[2];
        packet[0] = AcProtocol.GetCarInfo;

        for (var i = 0; i < _options.MaxCarSlots; i++)
        {
            packet[1] = (byte)i;
            await _udpClient.SendAsync(packet, _serverEndpoint);
        }

        logger.LogInformation("Requested CarInfo for slots 0–{Max}", _options.MaxCarSlots - 1);
    }

    private void ProcessPacket(byte[] data)
    {
        if (data.Length == 0)
            return;

        var packetType = data[0];

        switch (packetType)
        {
            case AcProtocol.NewSession:
            case AcProtocol.SessionInfo:
                HandleSessionInfo(data);
                break;

            case AcProtocol.EndSession:
                _events.Writer.TryWrite(new SimEventSessionEnded());
                break;

            case AcProtocol.NewConnection:
                HandleNewConnection(data);
                break;

            case AcProtocol.ConnectionClosed:
                HandleConnectionClosed(data);
                break;

            case AcProtocol.Version:
                logger.LogDebug("Version packet received (protocol handshake)");
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

            default:
                logger.LogDebug("Unhandled packet type: {PacketType}", packetType);
                break;
        }
    }

    private void HandleSessionInfo(byte[] data)
    {
        var info = AcPacketParser.ParseSessionInfo(data);
        var session = new SimEventSessionInfoReceived(
            info.ServerName,
            TrackNameSanitizer.Sanitize(info.Track),
            info.TrackConfig == string.Empty ? null : info.TrackConfig,
            (SessionType)info.Type,
            info.Time,
            info.Laps,
            info.ElapsedMs,
            info.AmbientTemp,
            info.RoadTemp
        );
        _events.Writer.TryWrite(session);
    }

    private void HandleNewConnection(byte[] data)
    {
        var conn = AcPacketParser.ParseNewConnection(data);
        var entry = new SimEventDriverConnected(
            conn.CarId,
            conn.CarModel,
            conn.CarSkin,
            conn.DriverName,
            conn.DriverGuid
        );

        _events.Writer.TryWrite(entry);
    }

    private void HandleConnectionClosed(byte[] data)
    {
        var conn = AcPacketParser.ParseConnectionClosed(data);
        var entry = new SimEventDriverDisconnected(
            conn.CarId,
            conn.CarModel,
            conn.CarModel,
            conn.DriverName,
            conn.DriverGuid
        );
        _events.Writer.TryWrite(entry);
    }

    private void HandleCarInfo(byte[] data)
    {
        var info = AcPacketParser.ParseCarInfo(data);
        if (!info.IsConnected)
            return;

        var entry = new SimEventCarInfoReceived(
            info.CarId,
            info.CarModel,
            info.CarSkin,
            info.DriverName,
            info.CarModel
        );

        _events.Writer.TryWrite(entry);
    }

    private void HandleCarUpdate(byte[] data)
    {
        var update = AcPacketParser.ParseCarUpdate(data);
        var v = update.Velocity;
        var speedKmh = MathF.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z) * 3.6f;

        var telemetry = new SimEventTelemetryUpdated(
            update.CarId,
            update.NormalizedSplinePos,
            update.Position.X,
            update.Position.Z,
            speedKmh,
            update.Gear,
            update.EngineRpm
        );

        _events.Writer.TryWrite(telemetry);
    }

    private void HandleLapCompleted(byte[] data)
    {
        var lapInfo = AcPacketParser.ParseLapCompleted(data);
        logger.LogDebug(
            "LapCompleted: CarId={CarId} LapTime={LapTime}ms Cuts={Cuts} Grip={Grip} Cars={Cars} Leaderboard=[{Leaderboard}]",
            lapInfo.CarId,
            lapInfo.LapTimeMs,
            lapInfo.Cuts,
            lapInfo.GripLevel,
            lapInfo.CarsCount,
            string.Join(
                ", ",
                lapInfo.Leaderboard.Select(e => $"Car{e.CarId}:{e.LapTimeMs}ms/{e.Laps}laps")
            )
        );

        var evt = new SimEventLapCompleted(
            lapInfo.CarId,
            lapInfo.LapTimeMs,
            lapInfo.Cuts,
            lapInfo
                .Leaderboard.Select(e => new LeaderBoardEntry(e.CarId, e.LapTimeMs, e.Laps))
                .ToList()
        );

        _events.Writer.TryWrite(evt);
    }

    private void HandleClientEvent(byte[] data)
    {
        var evt = AcPacketParser.ParseClientEvent(data);
        logger.LogInformation(
            "Collision: Type={Type} Car={CarId} OtherCar={OtherCarId} Speed={Speed:F1}km/h",
            evt.EventType,
            evt.CarId,
            evt.OtherCarId,
            evt.ImpactSpeed
        );

        var collision = new SimEventCollisionDetected(
            evt.CarId,
            (CollisionType)evt.EventType,
            evt.OtherCarId,
            evt.ImpactSpeed,
            DateTime.UtcNow
        );

        _events.Writer.TryWrite(collision);
    }

    // think about if we want to do this here..
    public async Task SendRealtimePosIntervalAsync()
    {
        if (_udpClient is null || _serverEndpoint is null || _options.RealtimePosIntervalMs <= 0)
            return;

        // RealtimePosInterval: byte(200) + uint16(interval ms)
        var packet = new byte[3];
        packet[0] = AcProtocol.RealtimePosInterval;
        BitConverter.TryWriteBytes(packet.AsSpan(1), (ushort)_options.RealtimePosIntervalMs);

        await _udpClient.SendAsync(packet, _serverEndpoint);
    }
}
