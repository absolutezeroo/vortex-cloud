using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>One page of the article list, as an editor filters it.</summary>
public sealed record ArticleListResponse(
    int Total,
    int Page,
    int PageSize,
    int Count,
    IReadOnlyList<ArticleListItem> Items
);
