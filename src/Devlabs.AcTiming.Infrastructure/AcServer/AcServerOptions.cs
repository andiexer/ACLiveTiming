namespace Devlabs.AcTiming.Infrastructure.AcServer;

public class AcServerOptions
{
    public const string SectionName = "AcServer";

    public int UdpPort { get; set; } = AcProtocol.DefaultPort;

    public string ServerHost { get; set; } = "";

    public int ServerPort { get; set; }

    public int RealtimePosIntervalMs { get; set; } = 100;

    /// <summary>Number of car slots to query on session start (0 to MaxCarSlots-1).</summary>
    public int MaxCarSlots { get; set; } = 50;
}
