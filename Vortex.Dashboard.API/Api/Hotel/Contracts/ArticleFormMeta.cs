using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// Everything the editor's form needs to draw itself: the categories and languages to choose from,
/// and where pictures come from.
/// </summary>
/// <remarks>
/// One read rather than two. The form needs both lists to render at all, and two round trips for
/// two small tables is two chances to draw half a form.
/// </remarks>
public sealed record ArticleFormMeta(
    IReadOnlyList<ArticleCategoryOption> Categories,
    IReadOnlyList<ArticleLanguageOption> Languages,
    string? ImageBase,
    IReadOnlyList<string> ImageDirectories,
    IReadOnlyList<string> BlockTypes
);
