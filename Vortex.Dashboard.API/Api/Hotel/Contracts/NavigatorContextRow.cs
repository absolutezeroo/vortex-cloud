using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// One navigator tab and the quick links under it.
/// </summary>
/// <param name="KnownCode">Whether the client recognises this search code. False is a tab the
/// client will not ask anything for.</param>
public sealed record NavigatorContextRow(
    int Id,
    string SearchCode,
    bool Visible,
    int QueryType,
    string QueryTypeLabel,
    int OrderNum,
    bool KnownCode,
    IReadOnlyList<NavigatorQuickLinkRow> QuickLinks
);
