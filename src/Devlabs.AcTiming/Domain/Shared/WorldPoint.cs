namespace Devlabs.AcTiming.Domain.Shared;

/// <summary>
/// A point in AC world space (metres). X = left/right, Z = forward/back.
/// </summary>
public sealed record WorldPoint(float X, float Z);
