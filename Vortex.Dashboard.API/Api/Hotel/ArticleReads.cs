using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Vortex.Dashboard.API.Api.Hotel.Contracts;
using Vortex.Dashboard.API.Infrastructure;
using Vortex.Database.Context;
using Vortex.Database.Entities.Web;
using Vortex.Observability.Configuration;

namespace Vortex.Dashboard.API.Api.Hotel;

/// <summary>
/// Read surface for the website's news. Every write lives in
/// <see cref="Operations.ArticleOperations"/> and goes through <c>IWebArticleAdminService</c>,
/// which owns the rules; nothing here validates anything.
/// </summary>
/// <remarks>
/// <para>
/// These reads deliberately show what the public API hides — drafts, articles scheduled for a future
/// date, every language including the untranslated ones. An editor's list that only showed what is
/// already live would be useless for the one job the page exists to do.
/// </para>
/// <para>
/// Three dependencies: a context, the asset URL builder for the picture base, and the observability
/// config for the asset root the picture browser walks.
/// </para>
/// </remarks>
internal sealed class ArticleReads(
    IDbContextFactory<VortexDbContext> dbContextFactory,
    DashboardAssetUrls assetUrls,
    IOptions<ObservabilityConfig> options
) : DashboardReads(dbContextFactory)
{
    private readonly DashboardAssetUrls _assetUrls = assetUrls;
    private readonly ObservabilityConfig _config = options.Value;

    /// <summary>Folders under the asset root an article may pick a picture from. A closed list: the
    /// parameter reaches the file system, and "whatever the caller typed" is how that becomes a
    /// directory traversal.</summary>
    private static readonly string[] ArticleImageDirectories = ["web_promo", "articles"];

    private const int ARTICLE_PAGE_SIZE = 25;
    private const int IMAGE_PAGE_SIZE = 60;

    /// <summary>
    /// The article list, filtered the way an editor filters: by status, by category, by language, or
    /// by a word in the title. Each row reports which languages it exists in, so a missing
    /// translation is visible without opening the article.
    /// </summary>
    public Task<ArticleListResponse> ArticlesAsync(
        NameValueCollection query,
        CancellationToken ct
    ) =>
        QueryAsync<ArticleListResponse>(
            async db =>
            {
                string status = (query["status"] ?? string.Empty).Trim();
                string category = (query["category"] ?? string.Empty).Trim();
                string language = (query["lang"] ?? string.Empty).Trim();
                string search = (query["q"] ?? string.Empty).Trim();
                int page = Math.Max(1, QueryValues.Int(query["page"], 1));

                IQueryable<WebArticleEntity> articles = db
                    .WebArticles.AsNoTracking()
                    .Where(a => a.DeletedAt == null);

                if (
                    status.Length > 0
                    && Enum.TryParse(status, ignoreCase: true, out WebArticleStatus parsedStatus)
                )
                {
                    articles = articles.Where(a => a.Status == parsedStatus);
                }

                if (category.Length > 0)
                {
                    articles = articles.Where(a =>
                        a.Category != null && a.Category.Code == category
                    );
                }

                if (language.Length > 0)
                {
                    articles = articles.Where(a =>
                        db.WebArticleTranslations.Any(t =>
                            t.ArticleId == a.Id && t.LanguageCode == language && t.DeletedAt == null
                        )
                    );
                }

                if (search.Length > 0)
                {
                    articles = articles.Where(a =>
                        a.Slug.Contains(search)
                        || db.WebArticleTranslations.Any(t =>
                            t.ArticleId == a.Id && t.DeletedAt == null && t.Title.Contains(search)
                        )
                    );
                }

                int total = await articles.CountAsync(ct).ConfigureAwait(false);

                List<WebArticleEntity> rows = await articles
                    .OrderByDescending(a => a.Pinned)
                    .ThenByDescending(a => a.PublishAt ?? a.CreatedAt)
                    .Skip((page - 1) * ARTICLE_PAGE_SIZE)
                    .Take(ARTICLE_PAGE_SIZE)
                    .Include(a => a.Category)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                List<int> ids = rows.ConvertAll(a => a.Id);

                var translations = await db
                    .WebArticleTranslations.AsNoTracking()
                    .Where(t => ids.Contains(t.ArticleId) && t.DeletedAt == null)
                    .Select(t => new
                    {
                        t.ArticleId,
                        t.LanguageCode,
                        t.Title,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                DateTime now = DateTime.UtcNow;

                List<ArticleListItem> items = rows.ConvertAll(a =>
                {
                    var mine = translations.Where(t => t.ArticleId == a.Id).ToList();

                    return new ArticleListItem(
                        a.Id,
                        a.Slug,
                        a.Category?.Code ?? string.Empty,
                        a.Status.ToString(),
                        // "Scheduled" is not a stored state -- it is Published with a date still
                        // ahead. The list says so because an editor otherwise sees "Published"
                        // beside an article nobody can read yet.
                        a.Status == WebArticleStatus.Published
                            && a.PublishAt != null
                            && a.PublishAt > now,
                        a.PublishAt,
                        a.Pinned,
                        a.AuthorName,
                        mine.Count > 0 ? mine[0].Title : string.Empty,
                        mine.ConvertAll(t => t.LanguageCode)
                    );
                });

                return new ArticleListResponse(total, page, ARTICLE_PAGE_SIZE, items.Count, items);
            },
            ct
        );

    /// <summary>One article with every translation it has, which is what the editor loads.</summary>
    public Task<ArticleDetail?> ArticleDetailAsync(int articleId, CancellationToken ct) =>
        QueryAsync<ArticleDetail?>(
            async db =>
            {
                WebArticleEntity? article = await db
                    .WebArticles.AsNoTracking()
                    .Include(a => a.Category)
                    .FirstOrDefaultAsync(a => a.Id == articleId && a.DeletedAt == null, ct)
                    .ConfigureAwait(false);

                if (article is null)
                {
                    return null;
                }

                List<ArticleTranslation> translations = await db
                    .WebArticleTranslations.AsNoTracking()
                    .Where(t => t.ArticleId == articleId && t.DeletedAt == null)
                    .OrderBy(t => t.LanguageCode)
                    .Select(t => new ArticleTranslation(
                        t.LanguageCode,
                        t.Title,
                        t.Summary,
                        t.BodyJson,
                        t.HeaderImage,
                        t.Thumbnail
                    ))
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                return new ArticleDetail(
                    article.Id,
                    article.Slug,
                    article.Category?.Code ?? string.Empty,
                    article.Status.ToString(),
                    article.PublishAt,
                    article.Pinned,
                    article.AuthorName,
                    translations
                );
            },
            ct
        );

    /// <summary>Categories and languages together: the editor needs both to draw a single form, and
    /// two round trips for two small tables is two chances to render half a form.</summary>
    public Task<ArticleFormMeta> ArticleFormMetaAsync(CancellationToken ct) =>
        QueryAsync<ArticleFormMeta>(
            async db =>
            {
                List<ArticleCategoryOption> categories = await db
                    .WebArticleCategories.AsNoTracking()
                    .Where(c => c.DeletedAt == null)
                    .OrderBy(c => c.SortOrder)
                    .ThenBy(c => c.Code)
                    .Select(c => new ArticleCategoryOption(
                        c.Id,
                        c.Code,
                        c.LabelJson,
                        c.SortOrder,
                        c.Enabled
                    ))
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                List<ArticleLanguageOption> languages = await db
                    .WebLanguages.AsNoTracking()
                    .Where(l => l.DeletedAt == null)
                    .OrderBy(l => l.SortOrder)
                    .ThenBy(l => l.Code)
                    .Select(l => new ArticleLanguageOption(
                        l.Id,
                        l.Code,
                        l.Label,
                        l.IsDefault,
                        l.Enabled,
                        l.SortOrder
                    ))
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                return new ArticleFormMeta(
                    categories,
                    languages,
                    _assetUrls.ArticleImageBase,
                    ArticleImageDirectories,
                    [
                        WebArticleBody.TypeParagraph,
                        WebArticleBody.TypeHeading,
                        WebArticleBody.TypeImage,
                        WebArticleBody.TypeButton,
                        WebArticleBody.TypeRule,
                    ]
                );
            },
            ct
        );

    /// <summary>
    /// The pictures an article can use, out of the asset root's <c>c_images</c> tree.
    /// </summary>
    /// <remarks>
    /// Searched and paged rather than listed: <c>web_promo</c> alone holds over three thousand files,
    /// and a grid of three thousand thumbnails is a page an editor closes. <c>.thumb.png</c> variants
    /// are folded into their main image, the same way the targeted-offer picker does it.
    /// Returns nothing when <c>AssetsLocalRoot</c> is unset — the form then takes a typed path.
    /// </remarks>
    public ArticleImageBrowse ArticleImages(NameValueCollection query)
    {
        string directory = (query["dir"] ?? ArticleImageDirectories[0]).Trim();

        int page = QueryValues.Page(query["page"]);

        if (!ArticleImageDirectories.Contains(directory, StringComparer.Ordinal))
        {
            return Nothing(page, "invalid_directory");
        }

        string root = _config.AssetsLocalRoot;

        if (string.IsNullOrWhiteSpace(root))
        {
            return Nothing(page);
        }

        string path = Path.Combine(root, "c_images", directory);

        if (!Directory.Exists(path))
        {
            return Nothing(page);
        }

        string search = (query["q"] ?? string.Empty).Trim();

        HashSet<string> all = Directory
            .EnumerateFiles(path)
            .Select(Path.GetFileName)
            .Where(name => name is not null)
            .Select(name => name!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<string> matches = all.Where(name =>
                (
                    name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                    || name.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)
                )
                && !name.EndsWith(".thumb.png", StringComparison.OrdinalIgnoreCase)
                && !name.EndsWith(".thumb.gif", StringComparison.OrdinalIgnoreCase)
                && (search.Length == 0 || name.Contains(search, StringComparison.OrdinalIgnoreCase))
            )
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        List<ArticleImage> items = matches
            .Skip((page - 1) * IMAGE_PAGE_SIZE)
            .Take(IMAGE_PAGE_SIZE)
            .Select(name =>
            {
                string thumbName =
                    Path.GetFileNameWithoutExtension(name) + ".thumb" + Path.GetExtension(name);

                return new ArticleImage(
                    $"/{directory}/{name}",
                    all.Contains(thumbName) ? $"/{directory}/{thumbName}" : $"/{directory}/{name}"
                );
            })
            .ToList();

        return new ArticleImageBrowse(matches.Count, page, IMAGE_PAGE_SIZE, items.Count, items);
    }

    /// <summary>
    /// An answer with no pictures in it, carrying the same fields as one that found some.
    /// </summary>
    /// <remarks>
    /// The three ways to find nothing used to answer three different shapes, and the picker made up
    /// the missing fields itself. It reads the same values off this.
    /// </remarks>
    private static ArticleImageBrowse Nothing(int page, string? error = null) =>
        new(0, page, IMAGE_PAGE_SIZE, 0, [], error);
}
