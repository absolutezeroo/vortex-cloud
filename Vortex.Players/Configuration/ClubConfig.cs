namespace Vortex.Players.Configuration;

/// <summary>
/// Strongly-typed options for Habbo Club / VIP. Only the maintenance timer interval stays bound via
/// IOptions (read once at grain activation to register the infra timer). The tunable membership
/// economics and granted badge codes are served live from <c>IServerConfigGrain</c>; the constants
/// below are the fallback defaults used when a key has no admin override stored in the DB.
/// </summary>
public sealed class ClubConfig
{
    public const string SECTION_NAME = "Vortex:Club";

    /// <summary>Interval between hourly maintenance runs (expiry, gift tokens, kickback payout).</summary>
    public int MaintenanceIntervalMs { get; init; } = 3_600_000;

    /// <summary>Badge code granted on the first Habbo Club purchase. Served live from <c>IServerConfigGrain</c>.</summary>
    public const string ClubBadgeCodeKey = "club.badge_code";
    public const string ClubBadgeCodeDefault = "HC1";

    /// <summary>Badge code granted while holding a VIP membership. Served live from <c>IServerConfigGrain</c>.</summary>
    public const string VipBadgeCodeKey = "club.vip_badge_code";
    public const string VipBadgeCodeDefault = "HC2";

    /// <summary>Length in days of one gift / payday cycle (also caps the streak payday bonus). Served live from <c>IServerConfigGrain</c>.</summary>
    public const string GiftCycleDaysKey = "club.gift_cycle_days";
    public const int GiftCycleDaysDefault = 31;

    /// <summary>
    /// Grace window in days after expiry within which a renewal keeps the membership streak.
    /// A longer lapse resets the streak so it starts over from the new purchase. Served live from
    /// <c>IServerConfigGrain</c>.
    /// </summary>
    public const string StreakGraceDaysKey = "club.streak_grace_days";
    public const int StreakGraceDaysDefault = 7;

    /// <summary>Percentage of credits spent this period returned as the Club kickback (payday). Served live from <c>IServerConfigGrain</c>.</summary>
    public const string KickbackPercentKey = "club.kickback_percent";
    public const int KickbackPercentDefault = 10;
}
