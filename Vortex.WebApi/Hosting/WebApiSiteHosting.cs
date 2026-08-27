using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Vortex.WebApi.Configuration;
using Vortex.WebApi.Services;

namespace Vortex.WebApi.Hosting;

/// <summary>
/// Serves the hotel's website off this same listener when
/// <see cref="WebApiConfig.SiteRoot"/> points at its built <c>dist/</c>, and adds the one server-side
/// page the site cannot produce for itself: the share URL that carries Open Graph tags.
/// </summary>
/// <remarks>
/// Same origin as <c>/api</c> is the whole point. The session is an HttpOnly cookie; a site served
/// from another origin would need CORS and would still not carry it on a plain fetch.
/// </remarks>
internal static class WebApiSiteHosting
{
    private const string IndexFile = "index.html";

    /// <summary>How many characters of the summary an OG description keeps. Facebook and Discord both
    /// cut around there, and a description truncated by the reader looks worse than one that ends.</summary>
    private const int DESCRIPTION_LIMIT = 200;

    public static void Map(WebApplication app, WebApiConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.SiteRoot))
        {
            return;
        }

        string root = Path.GetFullPath(Environment.ExpandEnvironmentVariables(config.SiteRoot));
        PhysicalFileProvider provider = new(root);

        app.UseStaticFiles(
            new StaticFileOptions { FileProvider = provider, RequestPath = PathString.Empty }
        );

        MapShareUrl(app, config, root);

        // Every other path is one of the SPA's own hash-less entry points; the router sorts it out
        // once index.html is loaded. Registered last so it never shadows /api or the share URL.
        app.MapFallback(async ctx =>
        {
            string? index = await ReadIndexAsync(root, ctx.RequestAborted).ConfigureAwait(false);

            if (index is null)
            {
                ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            await WriteHtmlAsync(ctx, index).ConfigureAwait(false);
        });
    }

    /// <summary>
    /// <c>GET /article/{slug}</c> — the URL an article is SHARED with.
    /// </summary>
    /// <remarks>
    /// The site routes on the hash, so a link like <c>site/#/article/x</c> sends only <c>site/</c> to
    /// the server and every shared article previews as the front page. This route answers with the
    /// same index.html, its head carrying that article's tags, then sends the browser on to the hash
    /// route. A crawler stops at the tags; a person never sees the difference.
    /// <para>
    /// <c>?lang=</c> is read before <c>Accept-Language</c>, and it is what makes the preview match
    /// the article that was shared. Discord, Facebook, X and Slack fetch a link with no meaningful
    /// <c>Accept-Language</c>, so a language read from the header alone would render every preview
    /// in the default language however the sharer was reading. The site is expected to put its
    /// current language on the URL it offers for copying.
    /// </para>
    /// </remarks>
    private static void MapShareUrl(WebApplication app, WebApiConfig config, string root)
    {
        app.MapGet(
                "/article/{slug}",
                async (
                    HttpContext ctx,
                    string slug,
                    IWebApiArticleService articles,
                    CancellationToken ct
                ) =>
                {
                    string? index = await ReadIndexAsync(root, ct).ConfigureAwait(false);

                    if (index is null)
                    {
                        ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    }

                    string? requested = ctx.Request.Query["lang"].ToString();

                    ArticleDetail? article = await articles
                        .GetArticleAsync(
                            slug,
                            string.IsNullOrWhiteSpace(requested)
                                ? ctx.AcceptedLanguages()
                                : requested,
                            ct
                        )
                        .ConfigureAwait(false);

                    // An unknown slug still serves the site: the SPA shows its own 404, which is a
                    // better page than anything this route could render.
                    string head = article is null
                        ? RedirectScript(slug)
                        : OpenGraphTags(ctx, config, article.Article) + RedirectScript(slug);

                    await WriteHtmlAsync(ctx, InjectIntoHead(index, head)).ConfigureAwait(false);
                }
            )
            .ExcludeFromDescription();
    }

    private static string OpenGraphTags(
        HttpContext ctx,
        WebApiConfig config,
        ArticleSummary article
    )
    {
        StringBuilder tags = new();

        Append(tags, "og:type", "article");
        Append(tags, "og:title", article.Title);
        Append(tags, "og:description", Shorten(article.Summary));
        Append(
            tags,
            "og:url",
            $"{ctx.Request.Scheme}://{ctx.Request.Host}{ctx.Request.Path}{ctx.Request.QueryString}"
        );

        // The hotel's name above the card, which is how a preview reads as coming from somewhere
        // rather than from a bare domain. Omitted when unset rather than guessed from the host.
        if (!string.IsNullOrWhiteSpace(config.SiteName))
        {
            Append(tags, "og:site_name", config.SiteName);
        }

        // No og:locale on purpose: it wants a language AND a territory (fr_FR), and web_languages
        // stores a bare code ("fr"). Emitting "fr" would be a malformed tag, and inventing FR for it
        // would be wrong for every hotel that publishes French outside France. It needs a territory
        // column first.

        if (article.PublishedAt.Length > 0)
        {
            Append(tags, "article:published_time", article.PublishedAt);
        }

        string image = AbsoluteImage(config, article.Image);

        if (image.Length > 0)
        {
            Append(tags, "og:image", image);
        }

        // Twitter reads og:* for everything but the card shape, which it only takes from its own
        // namespace — without this a link renders as a bare title instead of a picture card.
        tags.Append("<meta name=\"twitter:card\" content=\"summary_large_image\">");

        return tags.ToString();
    }

    private static void Append(StringBuilder tags, string property, string content) =>
        tags.Append("<meta property=\"")
            .Append(property)
            .Append("\" content=\"")
            .Append(WebUtility.HtmlEncode(content))
            .Append("\">");

    private static string Shorten(string summary) =>
        summary.Length <= DESCRIPTION_LIMIT
            ? summary
            : summary[..DESCRIPTION_LIMIT].TrimEnd() + "…";

    /// <summary>
    /// An article's picture as an absolute URL. Stored paths are relative to the asset host's
    /// <c>c_images</c> tree because that is how the site consumes them; a crawler has no such context
    /// and needs the whole thing.
    /// </summary>
    private static string AbsoluteImage(WebApiConfig config, string path)
    {
        if (string.IsNullOrEmpty(path) || string.IsNullOrWhiteSpace(config.AssetBaseUrl))
        {
            return string.Empty;
        }

        return $"{config.AssetBaseUrl.TrimEnd('/')}/c_images{path}";
    }

    /// <summary>
    /// Sends the browser to the SPA's own route for this article. Serialised as JSON so a slug can
    /// never break out of the string literal, whatever a future writer manages to store.
    /// </summary>
    private static string RedirectScript(string slug) =>
        "<script>location.replace("
        + JsonSerializer.Serialize($"/#/article/{slug}")
        + ");</script>";

    private static string InjectIntoHead(string html, string head)
    {
        int close = html.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);

        return close < 0 ? head + html : html.Insert(close, head);
    }

    private static async Task<string?> ReadIndexAsync(string root, CancellationToken ct)
    {
        string path = Path.Combine(root, IndexFile);

        // Read per request rather than cached: the file only changes on a redeploy, and a stale
        // index served until the next restart is a confusing bug to chase for a saving nobody would
        // measure on a route only crawlers and first loads hit.
        return File.Exists(path)
            ? await File.ReadAllTextAsync(path, ct).ConfigureAwait(false)
            : null;
    }

    private static Task WriteHtmlAsync(HttpContext ctx, string html)
    {
        ctx.Response.ContentType = "text/html; charset=utf-8";

        // index.html names hashed asset files, so caching it would pin a browser to a deployment
        // that no longer exists.
        ctx.Response.Headers.CacheControl = "no-cache";

        return ctx.Response.WriteAsync(html, ctx.RequestAborted);
    }
}
