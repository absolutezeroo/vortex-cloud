namespace Vortex.Primitives.Habbicons;

/// <summary>
/// How a player came to own a Habbicon. Server-side only — the client never sees it — but it is
/// what makes "why does this player have this?" answerable without reading the audit log, and what
/// a grant is deduplicated against when two sources could reasonably hand out the same Habbicon.
/// </summary>
public enum HabbiconSource
{
    Unknown = 0,

    /// <summary>Bought one at a time from the Habbicon shop.</summary>
    Shop = 1,

    /// <summary>Bought as a whole collection from the Habbicon shop.</summary>
    ShopCollection = 2,

    /// <summary>The bonus Habbicon of a completed collection, claimed by the player.</summary>
    CollectionReward = 3,

    /// <summary>A reward track prize.</summary>
    RewardTrack = 4,

    /// <summary>A quest, achievement or daily-task reward.</summary>
    Progression = 5,

    /// <summary>A campaign, event or promotional grant.</summary>
    Campaign = 6,

    /// <summary>Handed over by an operator from the dashboard.</summary>
    AdminGrant = 7,
}
