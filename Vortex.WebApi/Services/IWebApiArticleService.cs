using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Vortex.WebApi.Services;

/// <summary>A language the site publishes in, as the language picker needs it.</summary>
public sealed record SiteLanguage(string Code, string Label);

/// <summary>The enabled languages and which one is the fallback.</summary>
public sealed record SiteLanguages(string Default, IReadOnlyList<SiteLanguage> Items);

/// <summary>A category as the site's filter row shows it. <c>Id</c> is the code the feed filters on.</summary>
public sealed record ArticleCategory(string Id, string Label);

/// <summary>
/// One row of the news feed. The property names are the ones the site already reads in its
/// <c>src/lib/mock.js</c> — <c>id</c> is the slug, <c>image</c>/<c>thumbnail</c> are paths under
/// <c>c_images</c> that the site prefixes itself, and <c>date</c> is a plain ISO day.
/// </summary>
/// <param name="Date">
/// The publication day, no time and no zone, exactly as <c>mock.js</c> declares it. Kept because the
/// site's feed reads this key today.
/// </param>
/// <param name="PublishedAt">
/// The same moment as a full UTC instant (<c>2026-08-24T22:00:00.0000000Z</c>). <paramref name="Date"/>
/// alone cannot be rendered correctly by a reader in another zone: an article released at 23:00 in
/// Paris is already the next day in UTC and the previous one in São Paulo, and the site has no way to
/// recover the hour from a bare day. Added rather than substituted so the existing key keeps working.
/// </param>
public sealed record ArticleSummary(
    string Id,
    string Category,
    string Title,
    string Summary,
    string Image,
    string Thumbnail,
    string Date,
    string PublishedAt,
    string Author,
    bool Pinned,
    bool Fallback
);

/// <summary>An article's title and slug, for the "read also" list under an article.</summary>
public sealed record ArticleLink(string Id, string Title);

/// <summary>A feed page. <c>Lang</c> is the language actually served, which may not be the one asked
/// for — see <see cref="ArticleSummary.Fallback"/>.</summary>
public sealed record ArticleFeed(
    string Lang,
    int Page,
    int PageSize,
    int Total,
    IReadOnlyList<ArticleCategory> Categories,
    IReadOnlyList<ArticleSummary> Items
);

/// <summary>
/// A full article. <c>Body</c> is the stored block array, passed through as JSON rather than
/// re-modelled: the write side validated it against the closed block vocabulary, and re-shaping it
/// here would be a second place for the two ends to disagree.
/// </summary>
public sealed record ArticleDetail(
    string Lang,
    ArticleSummary Article,
    JsonArray Body,
    IReadOnlyList<ArticleLink> Related
);

/// <summary>
/// The website's editorial reads. Plain EF over the shared context, no grain: an article is content,
/// not live game state — nothing on the game socket owns it and nothing has to be kept coherent with
/// a room or a player.
/// </summary>
public interface IWebApiArticleService
{
    Task<SiteLanguages> GetLanguagesAsync(CancellationToken ct);

    Task<ArticleFeed> GetFeedAsync(
        string? category,
        string? requestedLanguage,
        int page,
        int pageSize,
        CancellationToken ct
    );

    Task<ArticleDetail?> GetArticleAsync(
        string slug,
        string? requestedLanguage,
        CancellationToken ct
    );
}
