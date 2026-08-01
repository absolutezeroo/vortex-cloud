namespace Vortex.Messages.Configuration;

/// <summary>
/// Per-session token-bucket limit on inbound packets, enforced by <c>RateLimitBehavior</c> before
/// any handler runs. Generous by default: this exists to close the cheapest DoS vector (a client
/// looping packets at wire speed), not to police normal play.
/// </summary>
public sealed class RateLimitConfig
{
    public const string SECTION_NAME = "Vortex:Networking:RateLimit";

    /// <summary>Sustained packets/second a session may send once its burst allowance is spent.</summary>
    public int MaxPacketsPerSecond { get; init; } = 50;

    /// <summary>
    /// Bucket capacity - how many packets a session may send in a single instant before the
    /// per-second refill rate starts gating it. Defaults to twice the sustained rate so a normal
    /// burst (e.g. bootstrap composers, a quick flurry of moves) is never affected.
    /// </summary>
    public int BurstSize { get; init; } = 100;
}
