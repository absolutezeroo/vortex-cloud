using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Web;

namespace Vortex.WebApi.Services;

/// <summary>
/// The public editorial reads. Everything here is anonymous and cacheable in principle; nothing is
/// cached yet, because a hotel's article table is small and an index on
/// <c>(status, publish_at)</c> answers the feed. Add a cache when a measurement asks for one.
/// </summary>
public sealed class WebApiArticleService(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    ILogger<WebApiArticleService> logger
) : IWebApiArticleService
{
    internal const int DEFAULT_PAGE_SIZE = 10;
    internal const int MAX_PAGE_SIZE = 50;
    private const int RELATED_COUNT = 3;

    /// <summary>Serves when the languages table is empty — a fresh install must still answer, and an
    /// empty picker is friendlier than a 500.</summary>
    private const string FALLBACK_LANGUAGE = "fr";

    private readonly IDbContextFactory<VortexDbContext> _db = dbCtxFactory;
    private readonly ILogger<WebApiArticleService> _logger = logger;

    public async Task<SiteLanguages> GetLanguagesAsync(CancellationToken ct)
    {
        await using VortexDbContext db = await _db.CreateDbContextAsync(ct).ConfigureAwait(false);

        List<WebLanguageEntity> languages = await LoadLanguagesAsync(db, ct).ConfigureAwait(false);

        return new SiteLanguages(
            DefaultCode(languages),
            languages.Select(l => new SiteLanguage(l.Code, l.Label)).ToList()
        );
    }

    public async Task<ArticleFeed> GetFeedAsync(
        string? category,
        string? requestedLanguage,
        int page,
        int pageSize,
        CancellationToken ct
    )
    {
        page = page < 1 ? 1 : page;
        pageSize =
            pageSize < 1 ? DEFAULT_PAGE_SIZE
            : pageSize > MAX_PAGE_SIZE ? MAX_PAGE_SIZE
            : pageSize;

        await using VortexDbContext db = await _db.CreateDbContextAsync(ct).ConfigureAwait(false);

        List<WebLanguageEntity> languages = await LoadLanguagesAsync(db, ct).ConfigureAwait(false);
        string defaultCode = DefaultCode(languages);
        string lang = Resolve(languages, requestedLanguage, defaultCode);

        IQueryable<WebArticleEntity> query = Live(db);

        if (!string.IsNullOrWhiteSpace(category) && category != "all" && category != "tout")
        {
            query = query.Where(a =>
                a.Category != null && a.Category.Code == category && a.Category.Enabled
            );
        }

        // An article nobody can read in either the asked-for language or the fallback is not a feed
        // row with holes in it — it is not a feed row at all.
        query = query.Where(a =>
            db.WebArticleTranslations.Any(t =>
                t.ArticleId == a.Id
                && t.DeletedAt == null
                && (t.LanguageCode == lang || t.LanguageCode == defaultCode)
            )
        );

        int total = await query.CountAsync(ct).ConfigureAwait(false);

        List<WebArticleEntity> articles = await query
            .OrderByDescending(a => a.Pinned)
            .ThenByDescending(a => a.PublishAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(a => a.Category)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        Dictionary<int, WebArticleTranslationEntity> chosen = await ChooseTranslationsAsync(
                db,
                articles.Select(a => a.Id).ToList(),
                lang,
                defaultCode,
                ct
            )
            .ConfigureAwait(false);

        List<ArticleSummary> items = articles
            .Where(a => chosen.ContainsKey(a.Id))
            .Select(a => ToSummary(a, chosen[a.Id], lang))
            .ToList();

        return new ArticleFeed(
            lang,
            page,
            pageSize,
            total,
            await LoadCategoriesAsync(db, lang, defaultCode, ct).ConfigureAwait(false),
            items
        );
    }

    public async Task<ArticleDetail?> GetArticleAsync(
        string slug,
        string? requestedLanguage,
        CancellationToken ct
    )
    {
        await using VortexDbContext db = await _db.CreateDbContextAsync(ct).ConfigureAwait(false);

        List<WebLanguageEntity> languages = await LoadLanguagesAsync(db, ct).ConfigureAwait(false);
        string defaultCode = DefaultCode(languages);
        string lang = Resolve(languages, requestedLanguage, defaultCode);

        // Archived is readable here and absent from the feed: an old article's URL keeps working,
        // which is what habbo.com does and what any link ever shared depends on.
        WebArticleEntity? article = await db
            .WebArticles.AsNoTracking()
            .Include(a => a.Category)
            .FirstOrDefaultAsync(
                a =>
                    a.Slug == slug
                    && a.DeletedAt == null
                    && (
                        a.Status == WebArticleStatus.Archived
                        || (
                            a.Status == WebArticleStatus.Published
                            && a.PublishAt != null
                            && a.PublishAt <= DateTime.UtcNow
                        )
                    ),
                ct
            )
            .ConfigureAwait(false);

        if (article is null)
        {
            return null;
        }

        Dictionary<int, WebArticleTranslationEntity> chosen = await ChooseTranslationsAsync(
                db,
                [article.Id],
                lang,
                defaultCode,
                ct
            )
            .ConfigureAwait(false);

        if (!chosen.TryGetValue(article.Id, out WebArticleTranslationEntity? translation))
        {
            return null;
        }

        return new ArticleDetail(
            lang,
            ToSummary(article, translation, lang),
            ParseBody(article.Slug, translation.BodyJson),
            await LoadRelatedAsync(db, article, lang, defaultCode, ct).ConfigureAwait(false)
        );
    }

    /// <summary>Articles the public may see in a feed: not deleted, published, and past their date.</summary>
    private static IQueryable<WebArticleEntity> Live(VortexDbContext db) =>
        db
            .WebArticles.AsNoTracking()
            .Where(a =>
                a.DeletedAt == null
                && a.Status == WebArticleStatus.Published
                && a.PublishAt != null
                && a.PublishAt <= DateTime.UtcNow
            );

    private static Task<List<WebLanguageEntity>> LoadLanguagesAsync(
        VortexDbContext db,
        CancellationToken ct
    ) =>
        db
            .WebLanguages.AsNoTracking()
            .Where(l => l.Enabled && l.DeletedAt == null)
            .OrderBy(l => l.SortOrder)
            .ThenBy(l => l.Code)
            .ToListAsync(ct);

    private static string DefaultCode(List<WebLanguageEntity> languages) =>
        languages.FirstOrDefault(l => l.IsDefault)?.Code
        ?? languages.FirstOrDefault()?.Code
        ?? FALLBACK_LANGUAGE;

    /// <summary>
    /// The asked-for language when it exists and is enabled, the default otherwise. An unknown or
    /// disabled code is never an error: a visitor whose browser asks for a language the hotel does
    /// not publish still gets the site.
    /// </summary>
    private static string Resolve(
        List<WebLanguageEntity> languages,
        string? requested,
        string defaultCode
    )
    {
        if (string.IsNullOrWhiteSpace(requested))
        {
            return defaultCode;
        }

        foreach (string candidate in Candidates(requested))
        {
            WebLanguageEntity? match = languages.Find(l =>
                string.Equals(l.Code, candidate, StringComparison.OrdinalIgnoreCase)
            );

            if (match is not null)
            {
                return match.Code;
            }
        }

        return defaultCode;
    }

    /// <summary>
    /// Splits what a caller sent into codes to try, best first. This accepts a bare <c>?lang=fr</c>
    /// and a full <c>Accept-Language: fr-CH,fr;q=0.9,en;q=0.8</c> alike, so the endpoints do not each
    /// carry their own parser. Quality values order the header already, and re-sorting on them buys
    /// nothing a hotel would notice.
    /// </summary>
    private static IEnumerable<string> Candidates(string requested)
    {
        foreach (string part in requested.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            string tag = part.Split(';')[0].Trim();

            if (tag.Length == 0 || tag == "*")
            {
                continue;
            }

            yield return tag;

            int dash = tag.IndexOf('-', StringComparison.Ordinal);

            if (dash > 0)
            {
                yield return tag[..dash];
            }
        }
    }

    /// <summary>
    /// One translation per article: the requested language when it exists, the default language
    /// otherwise. Loaded for the whole page in a single query — a per-article read here is what turns
    /// a ten-row feed into twenty round trips.
    /// </summary>
    private static async Task<Dictionary<int, WebArticleTranslationEntity>> ChooseTranslationsAsync(
        VortexDbContext db,
        List<int> articleIds,
        string lang,
        string defaultCode,
        CancellationToken ct
    )
    {
        Dictionary<int, WebArticleTranslationEntity> chosen = [];

        if (articleIds.Count == 0)
        {
            return chosen;
        }

        List<WebArticleTranslationEntity> translations = await db
            .WebArticleTranslations.AsNoTracking()
            .Where(t =>
                articleIds.Contains(t.ArticleId)
                && t.DeletedAt == null
                && (t.LanguageCode == lang || t.LanguageCode == defaultCode)
            )
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (WebArticleTranslationEntity translation in translations)
        {
            // The exact language always wins, whichever order the rows came back in; the default
            // only takes the slot while nothing better has claimed it.
            if (translation.LanguageCode == lang || !chosen.ContainsKey(translation.ArticleId))
            {
                chosen[translation.ArticleId] = translation;
            }
        }

        return chosen;
    }

    private static async Task<List<ArticleCategory>> LoadCategoriesAsync(
        VortexDbContext db,
        string lang,
        string defaultCode,
        CancellationToken ct
    )
    {
        List<WebArticleCategoryEntity> categories = await db
            .WebArticleCategories.AsNoTracking()
            .Where(c => c.Enabled && c.DeletedAt == null)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Code)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        // "tout" leads the row on habbo.fr and is not a stored category — it is the absence of a
        // filter, so it is built here rather than seeded as a row somebody could delete.
        List<ArticleCategory> result = [new ArticleCategory("tout", AllLabel(lang))];

        result.AddRange(
            categories.Select(c => new ArticleCategory(
                c.Code,
                ReadLabel(c.LabelJson, lang, defaultCode, c.Code)
            ))
        );

        return result;
    }

    private static string AllLabel(string lang) =>
        lang.StartsWith("fr", StringComparison.OrdinalIgnoreCase) ? "Tout" : "All";

    /// <summary>
    /// A category label out of its per-language dictionary, applying the one fallback rule the whole
    /// site uses. A malformed dictionary degrades to the category's own code rather than failing the
    /// request: a broken label must not take the feed down with it.
    /// </summary>
    private static string ReadLabel(string labelJson, string lang, string defaultCode, string code)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(labelJson);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return code;
            }

            if (
                document.RootElement.TryGetProperty(lang, out JsonElement exact)
                && exact.ValueKind == JsonValueKind.String
            )
            {
                return exact.GetString() ?? code;
            }

            if (
                document.RootElement.TryGetProperty(defaultCode, out JsonElement fallback)
                && fallback.ValueKind == JsonValueKind.String
            )
            {
                return fallback.GetString() ?? code;
            }

            return code;
        }
        catch (JsonException)
        {
            return code;
        }
    }

    private static async Task<List<ArticleLink>> LoadRelatedAsync(
        VortexDbContext db,
        WebArticleEntity article,
        string lang,
        string defaultCode,
        CancellationToken ct
    )
    {
        List<WebArticleEntity> sameCategory = await Live(db)
            .Where(a => a.Id != article.Id && a.CategoryId == article.CategoryId)
            .OrderByDescending(a => a.PublishAt)
            .Take(RELATED_COUNT)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        // A category with nothing else in it would leave the "read also" list empty, which reads as
        // a broken block rather than as a quiet category — so it is topped up from everything else.
        if (sameCategory.Count < RELATED_COUNT)
        {
            List<int> excluded = [article.Id, .. sameCategory.Select(a => a.Id)];

            sameCategory.AddRange(
                await Live(db)
                    .Where(a => !excluded.Contains(a.Id))
                    .OrderByDescending(a => a.PublishAt)
                    .Take(RELATED_COUNT - sameCategory.Count)
                    .ToListAsync(ct)
                    .ConfigureAwait(false)
            );
        }

        Dictionary<int, WebArticleTranslationEntity> chosen = await ChooseTranslationsAsync(
                db,
                sameCategory.Select(a => a.Id).ToList(),
                lang,
                defaultCode,
                ct
            )
            .ConfigureAwait(false);

        return sameCategory
            .Where(a => chosen.ContainsKey(a.Id))
            .Select(a => new ArticleLink(a.Slug, chosen[a.Id].Title))
            .ToList();
    }

    private static ArticleSummary ToSummary(
        WebArticleEntity article,
        WebArticleTranslationEntity translation,
        string lang
    ) =>
        new(
            article.Slug,
            article.Category?.Code ?? string.Empty,
            translation.Title,
            translation.Summary,
            translation.HeaderImage,
            // The feed's plate falls back to the full-width picture, which is what the site's
            // NewsList already does with `article.thumbnail ?? article.image`.
            string.IsNullOrEmpty(translation.Thumbnail)
                ? translation.HeaderImage
                : translation.Thumbnail,
            article.PublishAt?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty,
            ToInstant(article.PublishAt),
            article.AuthorName,
            article.Pinned,
            translation.LanguageCode != lang
        );

    /// <summary>
    /// The publication moment as a zoned ISO instant.
    /// <para>
    /// The kind is forced to UTC rather than trusted: the column is written in UTC, but MySQL hands
    /// a <c>DATETIME</c> back as <see cref="DateTimeKind.Unspecified"/>, and <c>"O"</c> on an
    /// unspecified value emits no <c>Z</c>. A reader's browser would then parse it as local time and
    /// shift the article by its own offset — the exact error this field exists to prevent.
    /// </para>
    /// </summary>
    private static string ToInstant(DateTime? publishAt) =>
        publishAt is null
            ? string.Empty
            : DateTime
                .SpecifyKind(publishAt.Value, DateTimeKind.Utc)
                .ToString("O", CultureInfo.InvariantCulture);

    /// <summary>
    /// The stored block array, passed through. A body that will not parse is served as an empty
    /// article rather than a 500: the write path validates it, so unparseable here means the row was
    /// edited outside the application, and one bad row must not take the page down.
    /// </summary>
    private JsonArray ParseBody(string slug, string bodyJson)
    {
        try
        {
            if (JsonNode.Parse(bodyJson) is JsonArray parsed)
            {
                return parsed;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "Article {Slug} has an unreadable body; serving it empty.",
                slug
            );
        }

        return [];
    }
}
