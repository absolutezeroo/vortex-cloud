using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// A page of the pictures an article may use.
/// </summary>
/// <remarks>
/// One shape for every outcome, including the ones that found nothing. This used to answer three
/// different shapes -- a refusal carrying only an error, an empty answer with no paging fields, and
/// a full page -- and the picker papered over the difference with a fallback at each read. The
/// values it computed are unchanged; what changed is that the response now says what it is.
/// </remarks>
/// <param name="Error">
/// Set only when the request itself was refused, which today means a directory outside the closed
/// list. Null on every answer that simply found nothing.
/// </param>
public sealed record ArticleImageBrowse(
    int Total,
    int Page,
    int PageSize,
    int Count,
    IReadOnlyList<ArticleImage> Items,
    string? Error = null
);
