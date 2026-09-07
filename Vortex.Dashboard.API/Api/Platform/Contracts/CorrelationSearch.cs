using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>
/// Everything one correlation id touched, gathered from the three journals that carry it.
/// </summary>
/// <remarks>
/// All three sections page on the same offset, because they are three views of one operation
/// rather than three independent lists.
/// </remarks>
public sealed record CorrelationSearch(
    string Term,
    int Page,
    int Limit,
    int Offset,
    IReadOnlyList<CorrelationAuditRow> Audit,
    IReadOnlyList<CorrelationLedgerRow> Ledger,
    IReadOnlyList<CorrelationItemRow> Items
) : DirectorySearch;
