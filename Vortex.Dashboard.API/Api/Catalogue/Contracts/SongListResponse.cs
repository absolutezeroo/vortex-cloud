using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>One page of the hotel's Trax songs.</summary>
public sealed record SongListResponse(
    int Total,
    int Page,
    int PageSize,
    IReadOnlyList<SongListItem> Items
);
