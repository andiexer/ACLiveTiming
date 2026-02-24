namespace Devlabs.AcTiming.Infrastructure.AcServer;

public static class TrackNameSanitizer
{
    /// <summary>
    /// Extracts the actual track name from a path-like string.
    /// Content Manager can create track names with path separators like:
    /// - cm_0/config/00/preset/ks_silverstone
    /// - \cm_0\config\00\preset\ks_silverstone
    /// This method returns only the last segment (e.g., "ks_silverstone").
    /// </summary>
    /// <param name="trackName">The track name or path string to sanitize.</param>
    /// <returns>The sanitized track name without path prefixes.</returns>
    public static string Sanitize(string trackName)
    {
        if (string.IsNullOrWhiteSpace(trackName))
        {
            return trackName;
        }

        const char forwardSlash = '/';
        const char backSlash = '\\';

        var lastSlashIndex = trackName.LastIndexOfAny(new[] { forwardSlash, backSlash });

        if (lastSlashIndex >= 0 && lastSlashIndex < trackName.Length - 1)
        {
            return trackName.Substring(lastSlashIndex + 1);
        }

        return trackName;
    }
}
