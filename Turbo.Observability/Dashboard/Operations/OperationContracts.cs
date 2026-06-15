namespace Turbo.Observability.Dashboard.Operations;

/// <summary>
/// Outcome of a dashboard admin operation. Always carries the correlation id so the operator can
/// trace the action across logs, audit records and grain activity.
/// </summary>
internal sealed record OperationResult(bool Ok, string CorrelationId, string Message)
{
    public static OperationResult Succeeded(string correlationId) => new(true, correlationId, "ok");

    public static OperationResult Failed(string correlationId) =>
        new(false, correlationId, "operation_failed");
}

/// <summary>Grant credits to a player's wallet. <paramref name="Reason"/> is mandatory and audited.</summary>
internal sealed record GiveCreditsRequest(int PlayerId, int Amount, string Reason);

/// <summary>Grant activity points of a given type to a player.</summary>
internal sealed record GiveActivityPointsRequest(int PlayerId, int Type, int Amount, string Reason);

/// <summary>Grant a furniture definition (optionally with extra data) to a player's inventory.</summary>
internal sealed record GiveFurnitureRequest(
    int PlayerId,
    int DefinitionId,
    string? ExtraData,
    string Reason
);

/// <summary>Force-disconnect a player by dropping their active session.</summary>
internal sealed record KickPlayerRequest(int PlayerId, string Reason);
