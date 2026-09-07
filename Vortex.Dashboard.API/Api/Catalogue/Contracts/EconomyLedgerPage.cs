using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>One page of the wallet ledger.</summary>
public sealed record EconomyLedgerPage(
    int Count,
    int Page,
    int Limit,
    int Total,
    int Offset,
    IReadOnlyList<EconomyLedgerEntry> Items
);
