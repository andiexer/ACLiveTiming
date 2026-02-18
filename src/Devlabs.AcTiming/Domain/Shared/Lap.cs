namespace Devlabs.AcTiming.Domain.Shared;

public class Lap
{
    public int Id { get; set; }

    public int SessionId { get; set; }
    public Session Session { get; set; } = null!;

    public int DriverId { get; set; }
    public Driver Driver { get; set; } = null!;

    public int CarId { get; set; }
    public Car Car { get; set; } = null!;

    public int TrackId { get; set; }
    public Track Track { get; set; } = null!;

    /// <summary>Lap time in milliseconds.</summary>
    public int LapTimeMs { get; set; }

    /// <summary>Sector/split times in milliseconds.</summary>
    public List<int> SectorTimesMs { get; set; } = [];

    /// <summary>Number of track-limit cuts during this lap.</summary>
    public int Cuts { get; set; }

    public DateTime RecordedAtUtc { get; set; }
}
