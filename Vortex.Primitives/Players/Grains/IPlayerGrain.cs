using System;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Orleans.Snapshots.Players;
using Vortex.Primitives.Players.Enums;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Primitives.Players.Grains;

public interface IPlayerGrain : IGrainWithIntegerKey
{
    public Task SetFigureAsync(string figure, AvatarGenderType gender, CancellationToken ct);
    public Task SetNameAsync(string name, CancellationToken ct);
    public Task SetMottoAsync(string text, CancellationToken ct);

    /// <summary>Persists the player's preferred chat-bubble style (SetChatStylePreference, header
    /// 2634). No-op when the style is unchanged so a repeated toggle doesn't touch the database.</summary>
    public Task SetChatStylePreferenceAsync(int chatStyle, CancellationToken ct);

    /// <summary>The player's persisted preferred chat-bubble style (0 = default). Surfaced back to the
    /// client in the account-preferences packet so the settings UI shows the saved selection.</summary>
    public Task<int> GetChatStylePreferenceAsync(CancellationToken ct);

    /// <summary>
    /// Adjusts the player's persisted achievement score by <paramref name="delta"/> and returns the
    /// new total. Owned here (not by the achievement grain) because the score is cached in this
    /// grain's state and surfaced on the profile.
    /// </summary>
    public Task<int> AddAchievementScoreAsync(int delta, CancellationToken ct);

    /// <summary>
    /// Consumes one of the player's daily respect points if any remain (resetting the budget on a new
    /// day). Returns true if a respect could be given, false if the daily limit is reached.
    /// </summary>
    public Task<bool> TryGiveRespectAsync(int dailyLimit, CancellationToken ct);

    /// <summary>Increments the player's total received respect and returns the new total.</summary>
    public Task<int> AddRespectReceivedAsync(CancellationToken ct);

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

    /// <summary>The player's persisted account preferences (volumes, chat/camera/invite toggles, UI
    /// flags) surfaced back to the client in the account-preferences packet on login so the settings
    /// dialog shows the saved selection.</summary>
    public Task<PlayerAccountPreferencesSnapshot> GetAccountPreferencesAsync(CancellationToken ct);

    /// <summary>Persists the three audio volumes (SetSoundSettings, header 3662). Values are clamped
    /// to 0..100.</summary>
    public Task SetSoundSettingsAsync(
        int uiVolume,
        int furniVolume,
        int traxVolume,
        CancellationToken ct
    );

    /// <summary>Persists whether free-flow (bubble) chat is disabled (SetChatPreferences, header 1149).</summary>
    public Task SetFreeFlowChatDisabledAsync(bool disabled, CancellationToken ct);

    /// <summary>Persists whether incoming room invites are ignored (SetIgnoreRoomInvites, header 1332).</summary>
    public Task SetRoomInvitesIgnoredAsync(bool ignored, CancellationToken ct);

    /// <summary>Persists whether the room camera stops following the avatar (SetRoomCameraPreferences,
    /// header 3917).</summary>
    public Task SetRoomCameraFollowDisabledAsync(bool disabled, CancellationToken ct);

    /// <summary>Persists the client UI-flags bitmask, e.g. expanded friend bar / room tools
    /// (SetUIFlags, header 3653).</summary>
    public Task SetUiFlagsAsync(int flags, CancellationToken ct);
}
