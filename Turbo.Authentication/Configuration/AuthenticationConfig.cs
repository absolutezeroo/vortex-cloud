namespace Turbo.Authentication.Configuration;

public sealed class AuthenticationConfig
{
    public const string SECTION_NAME = "Turbo:Authentication";

    /// <summary>
    /// Secret used for HMAC-SHA256 hashing of remote IP addresses in auth events.
    /// Keep this secret stable and rotate as needed.
    /// </summary>
    public string IpHashSecret { get; init; } = "local-dev-ip-hash-secret";
}
