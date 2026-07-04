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
