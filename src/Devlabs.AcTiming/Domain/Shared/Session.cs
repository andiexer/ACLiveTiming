namespace Devlabs.AcTiming.Domain.Shared;

public class Session : Entity
{
    public required string ServerName { get; set; }
    public SessionType Type { get; set; }

    public int TrackId { get; set; }
    public Track Track { get; set; } = null!;

    public int TimeLimitMinutes { get; set; }
    public int LapLimit { get; set; }
    public int AmbientTemp { get; set; }
    public int RoadTemp { get; set; }

    public DateTime StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public SessionClosedReason? ClosedReason { get; set; }

    public ICollection<Lap> Laps { get; set; } = [];
    public ICollection<Driver> Drivers { get; set; } = [];

    public bool IsOpen => EndedAtUtc is null;

    public static Session Open(
        Track track,
        string serverName,
        SessionType type,
        int timeLimitMinutes,
        int lapLimit,
        int ambientTemp,
        int roadTemp
    )
    {
        return new Session
        {
            Track = track,
            ServerName = serverName,
            Type = type,
            TimeLimitMinutes = timeLimitMinutes,
            LapLimit = lapLimit,
            AmbientTemp = ambientTemp,
            RoadTemp = roadTemp,
            StartedAtUtc = DateTime.UtcNow,
        };
    }

    public void Close(SessionClosedReason reason)
    {
        if (!IsOpen)
            return;
        EndedAtUtc = DateTime.UtcNow;
        ClosedReason = reason;
    }

    public bool ProbablySameSession(string trackName, string? trackConfig, SessionType type)
    {
        if (!IsOpen)
        {
            return false;
        }

        if (Track.Name == trackName && Track.Config == trackConfig && Type == type)
        {
            return true;
        }

        return false;
    }

    public void Abort()
    {
        EndedAtUtc = DateTime.UtcNow;
        ClosedReason = SessionClosedReason.SessionConflict;
    }
}
