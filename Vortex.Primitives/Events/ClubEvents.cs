namespace Vortex.Primitives.Events;

/// <summary>A player purchased or renewed Habbo Club / VIP.</summary>
public sealed record ClubPurchasedEvent(
    int PlayerId,
    int Months,
    bool IsVip,
    bool IsRenewal,
    int CreditCost,
    int TotalMonths
) : IEvent;

/// <summary>Club gift tokens were granted to a player when their gift cycle elapsed.</summary>
public sealed record ClubGiftTokenGrantedEvent(int PlayerId, int Tokens, int TotalAvailable)
    : IEvent;

/// <summary>A player claimed a club gift.</summary>
public sealed record ClubGiftClaimedEvent(int PlayerId, string ProductCode) : IEvent;

/// <summary>A player received their Club kickback (payday) reward.</summary>
public sealed record ClubPaydayEvent(int PlayerId, int Credits) : IEvent;

/// <summary>A player's Club membership lapsed (expired) while their grain was active.</summary>
public sealed record ClubExpiredEvent(int PlayerId, bool WasVip) : IEvent;
