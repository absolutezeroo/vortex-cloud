using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// One page of the rentable-space audit trail.
/// </summary>
/// <param name="ActiveRentals">Spaces rented right now, across the whole hotel -- not a count of
/// this page, and not of the window.</param>
public sealed record RentableSpaceAuditPage(
    int ActiveRentals,
    int Count,
    int Page,
    int Limit,
    int Total,
    int Offset,
    IReadOnlyList<RentableSpaceAuditEntry> Items
);
