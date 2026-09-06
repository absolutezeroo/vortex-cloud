using System;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

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

public sealed record DeleteArticleRequest(int ArticleId, string Reason) : IReasonedRequest;

/// <summary>
/// One language's text, saved in the same request as the article it belongs to.
/// </summary>
/// <remarks>
/// Together rather than as a second call because they are one act: somebody writes an article. Two
/// endpoints meant two buttons both labelled "Save", and no way for a writer to know which one kept
/// their paragraph.
/// <para>
/// <paramref name="Body"/> is the block array as JSON; it is validated against the closed block
/// vocabulary before storage, so an unknown block type or a <c>javascript:</c> link is refused here
/// rather than rendered on the public site.
/// </para>
/// </remarks>
public sealed record ArticleTranslationPayload(
    string Lang,
    string Title,
    string Summary,
    string Body,
    string HeaderImage,
    string Thumbnail
);

public sealed record DeleteArticleTranslationRequest(int ArticleId, string Lang, string Reason)
    : IReasonedRequest;

/// <summary><paramref name="Labels"/> is a per-language dictionary, e.g.
/// <c>{"fr":"Campagnes","en":"Campaigns"}</c>.</summary>
public sealed record ArticleCategoryRequest(
    int CategoryId,
    string Code,
    string Labels,
    int SortOrder,
    bool Enabled,
    string Reason
) : IReasonedRequest;

public sealed record DeleteArticleCategoryRequest(int CategoryId, string Reason) : IReasonedRequest;

public sealed record WebLanguageRequest(
    int LanguageId,
    string Code,
    string Label,
    bool IsDefault,
    bool Enabled,
    int SortOrder,
    string Reason
) : IReasonedRequest;

public sealed record DeleteWebLanguageRequest(int LanguageId, string Reason) : IReasonedRequest;
