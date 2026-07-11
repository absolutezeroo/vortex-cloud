using System;

namespace Turbo.Dashboard.API.Operations;

/// <summary>
/// Outcome of a dashboard admin operation. Always carries the correlation id so the operator can
/// trace the action across logs, audit records and grain activity.
/// </summary>
public sealed record OperationResult(bool Ok, string CorrelationId, string Message)
{
    public static OperationResult Succeeded(string correlationId) => new(true, correlationId, "ok");

    public static OperationResult Failed(
        string correlationId,
        string message = "operation_failed"
    ) => new(false, correlationId, message);
}

/// <summary>Grant credits to a player's wallet. <paramref name="Reason"/> is mandatory and audited.</summary>
public sealed record GiveCreditsRequest(int PlayerId, int Amount, string Reason);

/// <summary>Grant activity points of a given type to a player.</summary>
public sealed record GiveActivityPointsRequest(int PlayerId, int Type, int Amount, string Reason);

/// <summary>Grant a furniture definition (optionally with extra data) to a player's inventory.</summary>
public sealed record GiveFurnitureRequest(
    int PlayerId,
    int DefinitionId,
    string? ExtraData,
    string Reason
);

/// <summary>Force-disconnect a player by dropping their active session.</summary>
public sealed record KickPlayerRequest(int PlayerId, string Reason);

/// <summary>
/// Create a redeemable voucher code. <paramref name="CurrencyType"/> is 1=Credits, 2=Silver,
/// 3=Emeralds, 4=ActivityPoints (see <c>Turbo.Primitives.Players.Enums.Wallet.CurrencyType</c>);
/// <paramref name="ActivityPointType"/> is required only when <paramref name="CurrencyType"/> is
/// ActivityPoints. <paramref name="MaxRedemptions"/> null means unlimited (across different
/// players — each player may still redeem a given code only once).
/// </summary>
public sealed record CreateVoucherRequest(
    string Code,
    int CurrencyType,
    int? ActivityPointType,
    int Amount,
    int? MaxRedemptions,
    DateTime? ExpiresAt,
    string Reason
);

/// <summary>Deactivate a voucher code so it can no longer be redeemed.</summary>
public sealed record DeactivateVoucherRequest(string Code, string Reason);

/// <summary>
/// Suspend the player's linked account. Kept separate from <see cref="UnbanPlayerRequest"/> —
/// folding "lift" into this same request via a nullable field would be a footgun since the domain
/// method's <c>bannedUntil: null</c> means lift, not "no change". <paramref name="Permanent"/> true
/// ignores <paramref name="DurationSeconds"/>.
/// </summary>
public sealed record BanPlayerRequest(
    int PlayerId,
    bool Permanent,
    int? DurationSeconds,
    string Reason
);

/// <summary>Lift an active account ban.</summary>
public sealed record UnbanPlayerRequest(int PlayerId, string Reason);

/// <summary>Room-scoped mute. Only works while the target is currently present in a room — there is
/// no account-wide chat mute in this codebase.</summary>
public sealed record MutePlayerRequest(int PlayerId, int DurationSeconds, string Reason);

/// <summary>Lock the player's ability to trade. See <see cref="BanPlayerRequest"/> for the
/// permanent/duration semantics and why lift is a separate request type.</summary>
public sealed record TradingLockRequest(
    int PlayerId,
    bool Permanent,
    int? DurationSeconds,
    string Reason
);

/// <summary>Lift an active trading lock.</summary>
public sealed record TradingUnlockRequest(int PlayerId, string Reason);

/// <summary>Pick up one or more open CFH tickets for handling.</summary>
public sealed record PickCfhTicketsRequest(int[] IssueIds);

/// <summary>
/// Close one or more CFH tickets. <paramref name="Reason"/> is 1=Useless, 2=Sanctioned,
/// 3=Resolved (see <c>Turbo.Primitives.Moderation.CfhTicketCloseReason</c>). Closing does not itself
/// apply a sanction — sanction the reported player as a separate ban action if warranted.
/// </summary>
public sealed record CloseCfhTicketsRequest(int[] IssueIds, int Reason, bool Sanctioned);

/// <summary>Release one or more picked tickets back to the open queue.</summary>
public sealed record ReleaseCfhTicketsRequest(int[] IssueIds);

/// <summary>Force-deactivate an active room. Does not itself evict occupants — pair with
/// <see cref="KickFromRoomRequest"/> per player if a hard clear is needed.</summary>
public sealed record ForceCloseRoomRequest(int RoomId, string Reason);

/// <summary>Remove one player from a room they are currently in. One-time removal, not a ban —
/// use <see cref="BanPlayerRequest"/> for account-wide sanctions.</summary>
public sealed record KickFromRoomRequest(int RoomId, int PlayerId, string Reason);
