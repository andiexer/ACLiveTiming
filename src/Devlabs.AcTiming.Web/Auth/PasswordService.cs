namespace Devlabs.AcTiming.Web.Auth;

public class PasswordService
{
    private readonly string _storedHash;

    public PasswordService(IConfiguration config, ILogger<PasswordService> logger)
    {
        var hash = config["Auth:PasswordHash"] ?? "";
        var plain = config["Auth:Password"] ?? "";

        if (!string.IsNullOrWhiteSpace(hash))
        {
            if (!string.IsNullOrWhiteSpace(plain))
            {
                logger.LogWarning(
                    "Auth: both plaintext password and password hash are configured. Ignoring Auth:Password. Remove Auth:Password from appsettings for better security."
                );
            }
            _storedHash = hash;
        }
        else if (!string.IsNullOrWhiteSpace(plain))
        {
            _storedHash = PasswordHasher.Hash(plain);
            logger.LogWarning(
                "Auth: plaintext password is in use. Set Auth:PasswordHash = \"{Hash}\" in appsettings and remove Auth:Password.",
                _storedHash
            );
        }
        else
        {
            throw new InvalidOperationException(
                "No authentication password configured. Set Auth:PasswordHash in appsettings."
            );
        }
    }

    public bool Verify(string password) => PasswordHasher.Verify(password, _storedHash);
}
