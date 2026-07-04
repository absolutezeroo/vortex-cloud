namespace Turbo.Players.Configuration;

/// <summary>
/// Strongly-typed options for Habbo Club / VIP, bound from the <c>Turbo:Club</c> configuration
/// section following the project's per-module options convention. Lets operators tune membership
/// economics and the granted badge codes without code changes.
/// </summary>
public sealed class ClubConfig
{
    public const string SECTION_NAME = "Turbo:Club";

    /// <summary>Badge code granted on the first Habbo Club purchase.</summary>
    public string ClubBadgeCode { get; init; } = "HC1";

    /// <summary>Badge code granted while holding a VIP membership.</summary>
    public string VipBadgeCode { get; init; } = "HC2";

    /// <summary>Length in days of one gift / payday cycle (also caps the streak payday bonus).</summary>
    public int GiftCycleDays { get; init; } = 31;

    /// <summary>
    /// Grace window in days after expiry within which a renewal keeps the membership streak.
    /// A longer lapse resets the streak so it starts over from the new purchase.
    /// </summary>
    public int StreakGraceDays { get; init; } = 7;

    /// <summary>Percentage of credits spent this period returned as the Club kickback (payday).</summary>
    public int KickbackPercent { get; init; } = 10;

    /// <summary>Interval between hourly maintenance runs (expiry, gift tokens, kickback payout).</summary>
    public int MaintenanceIntervalMs { get; init; } = 3_600_000;
}
