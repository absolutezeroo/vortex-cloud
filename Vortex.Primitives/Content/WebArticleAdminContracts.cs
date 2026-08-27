using System;

namespace Vortex.Primitives.Content;

/// <summary>
/// The editorial half of an article: when it comes out, where it is filed, whether it leads the page.
/// Nothing written lives here — that is <see cref="WebArticleTranslationSpec"/>, one per language.
/// </summary>
public sealed record WebArticleSpec(
    string Slug,
    string CategoryCode,
    WebArticleStatusSpec Status,
    DateTime? PublishAt,
    bool Pinned,
    string AuthorName
);

/// <summary>Mirrors the stored status. Kept out of the database assembly so the dashboard can talk
/// about an article without referencing the entity.</summary>
public enum WebArticleStatusSpec
{
    Draft = 0,
    Published = 1,
    Archived = 2,
}

/// <summary>
/// One article as written in one language. <paramref name="BodyJson"/> is the block array; it is
/// validated against the closed block vocabulary before anything is stored, so no markup can reach
/// the public site.
/// </summary>
public sealed record WebArticleTranslationSpec(
    string Title,
    string Summary,
    string BodyJson,
    string HeaderImage,
    string Thumbnail
);

public sealed record WebArticleCategorySpec(
    string Code,
    string LabelJson,
    int SortOrder,
    bool Enabled
);

public sealed record WebLanguageSpec(
    string Code,
    string Label,
    bool IsDefault,
    bool Enabled,
    int SortOrder
);

/// <summary>
/// What a write did. <paramref name="Error"/> carries the same code the HTTP layer returns
/// (<c>invalid_body</c>, <c>slug_taken</c>, …) so the reason a save was refused survives the trip to
/// the operator instead of collapsing into "failed".
/// </summary>
public sealed record WebArticleAdminResult(bool Success, string? Error = null, int Id = 0)
{
    public static WebArticleAdminResult Ok(int id = 0) => new(true, null, id);

    public static WebArticleAdminResult Fail(string error) => new(false, error);
}
