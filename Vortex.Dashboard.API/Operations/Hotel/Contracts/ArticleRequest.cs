using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Hotel.Contracts;

/// <summary>
/// Request bodies for the website's news operations, each carrying the mandatory audited
/// <c>Reason</c> every dashboard write carries.
/// </summary>
/// <remarks>
/// The article and its translations are separate requests because they are separate decisions: one
/// says when a piece comes out and where it is filed, the other is somebody writing in one language.
/// Editing a French paragraph should not have to restate a publication date.
/// </remarks>
public sealed record ArticleRequest(
    int ArticleId,
    string Slug,
    string Category,
    string Status,
    DateTime? PublishAt,
    bool Pinned,
    string Author,
    string Reason,
    ArticleTranslationPayload? Translation = null
) : IReasonedRequest;
