using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>One page of the audit spine.</summary>
public sealed record AuditPage(
    int Count,
    int Page,
    int Limit,
    int Total,
    int Offset,
    IReadOnlyList<AuditEntry> Items
);
