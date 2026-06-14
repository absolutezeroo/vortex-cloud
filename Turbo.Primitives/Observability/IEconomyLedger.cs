namespace Turbo.Primitives.Observability;

/// <summary>
/// Durable, append-only ledger of currency movements. Distinct from <see cref="IAuditSink"/>: every
/// entry records a signed delta and the resulting balance, enabling reconciliation and duplication
/// detection (the sum of deltas must match wallet balances). Implementations are non-blocking.
/// </summary>
public interface IEconomyLedger
{
    void Record(in EconomyLedgerEvent entry);
}
