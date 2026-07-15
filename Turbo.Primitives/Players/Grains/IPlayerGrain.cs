using System;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Primitives.Orleans.Snapshots.Players;
using Turbo.Primitives.Players.Enums;
using Turbo.Primitives.Rooms.Enums;

namespace Turbo.Primitives.Players.Grains;

public interface IPlayerGrain : IGrainWithIntegerKey
{
    public Task SetFigureAsync(string figure, AvatarGenderType gender, CancellationToken ct);
    public Task SetNameAsync(string name, CancellationToken ct);
    public Task SetMottoAsync(string text, CancellationToken ct);
    public Task<PlayerSummarySnapshot> GetSummaryAsync(CancellationToken ct);

    public Task<PlayerExtendedProfileSnapshot> GetExtendedProfileSnapshotAsync(
        CancellationToken ct
    );

    public Task<ClubSubscriptionSnapshot> GetClubSubscriptionAsync(CancellationToken ct);

    public Task<ClubPurchaseResult> PurchaseClubAsync(
        int months,
        bool isVip,
        int costCredits,
        CancellationToken ct
    );

    public Task<bool> TryConsumeClubGiftAsync(string productCode, CancellationToken ct);
    public Task TrackCreditSpendAsync(int credits, CancellationToken ct);

    /// <summary>
    /// Suspends (or, with <paramref name="bannedUntil"/> null, lifts) the linked account's ability
    /// to log in. Returns false if this player has no linked account to ban (e.g. a system player).
    /// </summary>
    public Task<bool> ApplyAccountBanAsync(
        int actorPlayerId,
        DateTime? bannedUntil,
        string reason,
        CancellationToken ct
    );

    /// <summary>Locks (or, with <paramref name="lockedUntil"/> null, lifts) the linked account's
    /// ability to trade. Returns false if this player has no linked account.</summary>
    public Task<bool> ApplyTradingLockAsync(
        int actorPlayerId,
        DateTime? lockedUntil,
        CancellationToken ct
    );

    /// <summary>Null if not currently banned, else the account's active ban expiry (far-future = permanent).</summary>
    public Task<DateTime?> GetActiveBanExpiryAsync(CancellationToken ct);

    public Task<PlayerWiredPreferencesSnapshot> GetWiredPreferencesAsync(CancellationToken ct);

    public Task SetWiredPreferencesAsync(
        PlayerWiredPreferencesSnapshot preferences,
        CancellationToken ct
    );
}
