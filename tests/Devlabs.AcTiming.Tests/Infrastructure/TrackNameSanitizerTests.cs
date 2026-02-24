namespace Devlabs.AcTiming.Tests.Infrastructure;

public class TrackNameSanitizerTests
{
    [Theory]
    [InlineData("cm_0/config/00/preset/ks_silverstone", "ks_silverstone")]
    [InlineData(@"\cm_0\config\00\preset\ks_silverstone", "ks_silverstone")]
    [InlineData("ks_silverstone", "ks_silverstone")]
    public void Sanitize_ShouldReturnExpectedTrackName(string input, string expected)
    {
        // Act
        var result = AcTiming.Infrastructure.AcServer.TrackNameSanitizer.Sanitize(input);

        // Assert
        Assert.Equal(expected, result);
    }
}
