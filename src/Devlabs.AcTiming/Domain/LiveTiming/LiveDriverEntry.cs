namespace Devlabs.AcTiming.Domain.LiveTiming;

public record LiveDriverEntry
{
    public int CarId { get; init; }
    public required string DriverName { get; init; }
    public required string DriverGuid { get; init; }
    public string? Team { get; init; }
    public required string CarModel { get; init; }
    public string? CarSkin { get; init; }
    public bool IsConnected { get; init; }

    public int? BestLapTimeMs { get; init; }
    public int? LastLapTimeMs { get; init; }
    public int TotalLaps { get; init; }
    public int Position { get; init; }

    /// <summary>Normalized spline position on track (0.0 - 1.0).</summary>
    public float SplinePosition { get; init; }

    /// <summary>World X position in meters.</summary>
    public float WorldX { get; init; }

    /// <summary>World Z position in meters.</summary>
    public float WorldZ { get; init; }

    /// <summary>Current speed in km/h, derived from velocity vector.</summary>
    public float SpeedKmh { get; init; }

    /// <summary>Current gear (0 = reverse, 1 = neutral, 2+ = gears).</summary>
    public int Gear { get; init; }

    /// <summary>Current engine RPM.</summary>
    public int EngineRpm { get; init; }

    /// <summary>Number of cuts (corner cuts) in the last completed lap. 0 = valid lap.</summary>
    public int LastLapCuts { get; init; }

    public List<int> LastSectorTimesMs { get; init; } = [];
    public List<int> BestSectorTimesMs { get; init; } = [];
}
