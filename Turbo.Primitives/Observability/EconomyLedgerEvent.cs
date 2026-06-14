namespace Turbo.Primitives.Observability;

/// <summary>Why a currency movement happened. The business context is linked via the correlation id.</summary>
public enum EconomyReason
{
    Debit,
    Grant,
    Adjustment,
}

/// <summary>
/// A single currency movement for one player. Decoupled from the game's wallet types: the caller maps
/// its currency kind to a plain label (and optional activity-point type) so the ledger stays a stable
/// contract. The correlation id and timestamp are stamped by the sink at record time.
/// </summary>
public readonly record struct EconomyLedgerEvent
{
    public required long PlayerId { get; init; }

    /// <summary>Currency label, for example "Credits", "Silver" or "ActivityPoints".</summary>
    public required string Currency { get; init; }

    /// <summary>Activity-point sub-type when <see cref="Currency"/> is activity points; otherwise null.</summary>
    public int? ActivityPointType { get; init; }

    /// <summary>Signed amount: negative for a debit, positive for a grant.</summary>
    public required long Delta { get; init; }

    /// <summary>Resulting balance after the movement was applied.</summary>
    public required long BalanceAfter { get; init; }

    public required EconomyReason Reason { get; init; }

    /// <summary>Optional reference to the originating entity (offer id, trade id, ...).</summary>
    public long? RefId { get; init; }
}
