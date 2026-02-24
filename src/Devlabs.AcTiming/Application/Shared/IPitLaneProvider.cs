namespace Devlabs.AcTiming.Application.Shared;

public interface IPitLaneProvider
{
    (float WorldX, float WorldZ)[]? LoadPoints(string trackName, string? trackConfig);
}
