namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// One language's version of an article.
/// </summary>
/// <param name="Body">
/// Handed over as the stored string. The editor parses it; re-shaping it here would be a second
/// opinion on a format that already has one owner.
/// </param>
public sealed record ArticleTranslation(
    string Lang,
    string Title,
    string Summary,
    string Body,
    string? HeaderImage,
    string? Thumbnail
);
