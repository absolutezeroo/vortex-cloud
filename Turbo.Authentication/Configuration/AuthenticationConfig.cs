namespace Turbo.Authentication.Configuration;

public sealed class AuthenticationConfig
{
    public const string SECTION_NAME = "Turbo:Authentication";

    /// <summary>
    /// Secret used for HMAC-SHA256 hashing of remote IP addresses in auth events.
    /// Keep this secret stable and rotate as needed.
    /// </summary>
    public string IpHashSecret { get; init; } = "local-dev-ip-hash-secret";

    /// <summary>
    /// Optional account email granted the <c>owner</c> role at startup. Use it to bootstrap the first
    /// administrator (e.g. for the admin dashboard) so there is never a lock-out before any role is
    /// assigned. Empty disables the bootstrap; the assignment is idempotent and additive-only.
    /// </summary>
    public string BootstrapOwnerEmail { get; init; } = string.Empty;

    /// <summary>
    /// Fallback TTL (seconds) applied when a ticket has no <c>expires_at</c> column value.
    /// Tickets older than this are rejected as expired. Set to 0 to disable the fallback and
    /// accept null-expiry tickets unconditionally (legacy / external CMS compatibility).
    /// Default: 30 seconds — tight enough to prevent replay, long enough for slow connections.
    /// </summary>
    public int TicketTtlSeconds { get; init; } = 30;
}
