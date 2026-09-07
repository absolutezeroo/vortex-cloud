using System;

namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// One movement of one wallet.
/// </summary>
/// <param name="Currency">The hotel's own name for it, not an enum: currencies are renamed in
/// <c>currency_types</c>.</param>
/// <param name="ActivityPointType">Which point type, for the currencies that are points. Null for
/// credits and for anything the ledger did not qualify.</param>
/// <param name="Delta">Signed: negative is spend.</param>
/// <param name="RefId">What the movement was about, when the writer had one id to name.</param>
public sealed record EconomyLedgerEntry(
    int Id,
    DateTime OccurredAt,
    long PlayerId,
    string? PlayerName,
    string Currency,
    int? ActivityPointType,
    long Delta,
    long BalanceAfter,
    string Reason,
    long? RefId,
    string? CorrelationId
);
