using Devlabs.AcTiming.Domain.LiveTiming;

namespace Devlabs.AcTiming.Domain.Shared;

public class Lap : Entity
{
    public int SessionId { get; set; }
    public Session Session { get; set; } = null!;

    public int DriverId { get; set; }
    public Driver Driver { get; set; } = null!;

    public int CarId { get; set; }
    public Car Car { get; set; } = null!;

    public int TrackId { get; set; }
    public Track Track { get; set; } = null!;

    /// <summary>1-indexed lap number for this driver+car combination within the session.</summary>
    public int LapNumber { get; set; }

    /// <summary>Lap time in milliseconds.</summary>
    public int LapTimeMs { get; set; }

    /// <summary>Sector/split times in milliseconds. Empty if no sector data was available.</summary>
    public List<int> SectorTimesMs { get; set; } = [];

    /// <summary>Number of track-limit cuts during this lap.</summary>
    public int Cuts { get; set; }

    /// <summary>True when Cuts == 0 (no track-limit violations).</summary>
    public bool IsValid { get; set; }

    /// <summary>Maximum speed recorded during this lap in km/h.</summary>
    public float MaxSpeedKmh { get; set; }

    public DateTime RecordedAtUtc { get; set; }

    /// <summary>
    /// Telemetry samples collected during this lap. Stored as a JSON column.
    /// May be empty if the lap was very short or telemetry data was unavailable.
    /// </summary>
    public List<LapTelemetrySample> TelemetrySamples { get; set; } = [];
}
