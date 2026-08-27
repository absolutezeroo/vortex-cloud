using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Database.Entities.Web;
using Xunit;

namespace Vortex.WebApi.Tests;

/// <summary>
/// <c>GET /article/{slug}</c>, the URL an article is shared with. The spec called for these cases and
/// they did not exist: <c>WebApiSiteHosting.Map</c> was only ever called from the production host, so
/// the route was not mapped under the test server and nothing here could be reached.
/// </summary>
/// <remarks>
/// What this route gets wrong is invisible to everyone except the person who pasted the link — the
/// site itself renders fine either way, and the failure shows up as a wrong preview in someone
/// else's chat window.
/// </remarks>
public sealed class ArticleShareUrlTests : IDisposable
{
    private const string Body = """[{"type":"p","text":"Hello"}]""";
    private const string Slug = "abobbados";

    private readonly string _siteRoot;

    public ArticleShareUrlTests()
    {
        _siteRoot = Path.Combine(Path.GetTempPath(), $"vortex-site-{Guid.NewGuid():N}");

        Directory.CreateDirectory(_siteRoot);
        File.WriteAllText(
            Path.Combine(_siteRoot, "index.html"),
            "<html><head><title>Vortex</title></head><body></body></html>"
        );
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_siteRoot, recursive: true);
        }
        catch (IOException)
        {
            // A leftover temp folder is not worth failing a test over.
        }
    }

    [Fact]
    public async Task ItCarriesTheArticleInOpenGraphTags()
    {
        await using WebApiTestFactory factory = NewFactory();
        await SeedAsync(factory);

        string html = await GetHtmlAsync(factory.Client, $"/article/{Slug}");

        html.Should().Contain("""<meta property="og:title" content="abobbados fr">""");
        html.Should().Contain("""<meta property="og:type" content="article">""");
        html.Should().Contain("""<meta property="og:site_name" content="Vortex Hotel">""");

        // The redirect still runs, so a person lands in the SPA's own route.
        html.Should().Contain("/#/article/abobbados");
    }

    [Fact]
    public async Task TheLanguageOnTheUrlWins_BecauseACrawlerSendsNoUsefulHeader()
    {
        // Discord, Facebook, X and Slack fetch a shared link with no meaningful Accept-Language.
        // Reading the language from the header alone rendered every preview in the default language
        // however the sharer was reading, which is the bug this parameter exists to close.
        await using WebApiTestFactory factory = NewFactory();
        await SeedAsync(factory, translateEnglish: true);

        string html = await GetHtmlAsync(factory.Client, $"/article/{Slug}?lang=en");

        html.Should().Contain("abobbados en");
        html.Should().NotContain("abobbados fr");
    }

    [Fact]
    public async Task WithoutTheParameter_TheHeaderStillDecides()
    {
        await using WebApiTestFactory factory = NewFactory();
        await SeedAsync(factory, translateEnglish: true);

        using HttpRequestMessage request = new(HttpMethod.Get, $"/article/{Slug}");
        request.Headers.Add("Accept-Language", "en");

        HttpResponseMessage response = await factory.Client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        (await response.Content.ReadAsStringAsync()).Should().Contain("abobbados en");
    }

    [Fact]
    public async Task ThePublicationInstantIsZoned()
    {
        // A bare "2026-08-24" cannot be rendered correctly by a reader in another zone, and a
        // "2026-08-24T22:00:00" with no Z is parsed as local time and silently shifted.
        await using WebApiTestFactory factory = NewFactory();
        await SeedAsync(factory);

        string html = await GetHtmlAsync(factory.Client, $"/article/{Slug}");

        html.Should().Contain("""<meta property="article:published_time" content=""");
        html.Should().Contain("Z\">");
    }

    [Fact]
    public async Task AnUnknownSlugStillServesTheSite()
    {
        await using WebApiTestFactory factory = NewFactory();
        await SeedAsync(factory);

        HttpResponseMessage response = await factory.Client.GetAsync("/article/nope");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().NotContain("og:title");
    }

    private WebApiTestFactory NewFactory() =>
        new(config =>
        {
            config.SiteRoot = _siteRoot;
            config.SiteName = "Vortex Hotel";
            config.AssetBaseUrl = "http://assets.test";
        });

    private static async Task<string> GetHtmlAsync(HttpClient client, string url)
    {
        HttpResponseMessage response = await client.GetAsync(url);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    private static async Task SeedAsync(WebApiTestFactory factory, bool translateEnglish = false)
    {
        await using VortexDbContext db = await factory.DbContexts.CreateDbContextAsync();

        db.WebLanguages.Add(
            new WebLanguageEntity
            {
                Code = "fr",
                Label = "Français",
                IsDefault = true,
                Enabled = true,
            }
        );
        db.WebLanguages.Add(
            new WebLanguageEntity
            {
                Code = "en",
                Label = "English",
                IsDefault = false,
                Enabled = true,
                SortOrder = 1,
            }
        );
        db.WebArticleCategories.Add(
            new WebArticleCategoryEntity
            {
                Code = "campagnes",
                LabelJson = """{"fr":"Campagnes"}""",
                Enabled = true,
            }
        );

        await db.SaveChangesAsync();

        WebArticleEntity entity = new()
        {
            Slug = Slug,
            CategoryId = await db.WebArticleCategories.Select(c => c.Id).FirstAsync(),
            Status = WebArticleStatus.Published,
            PublishAt = DateTime.UtcNow.AddHours(-1),
            AuthorName = "Vortex",
        };

        db.WebArticles.Add(entity);
        await db.SaveChangesAsync();

        db.WebArticleTranslations.Add(
            new WebArticleTranslationEntity
            {
                ArticleId = entity.Id,
                LanguageCode = "fr",
                Title = $"{Slug} fr",
                Summary = "résumé",
                BodyJson = Body,
                HeaderImage = "/web_promo/x.png",
            }
        );

        if (translateEnglish)
        {
            db.WebArticleTranslations.Add(
                new WebArticleTranslationEntity
                {
                    ArticleId = entity.Id,
                    LanguageCode = "en",
                    Title = $"{Slug} en",
                    Summary = "summary",
                    BodyJson = Body,
                    HeaderImage = "/web_promo/x.png",
                }
            );
        }

        await db.SaveChangesAsync();
    }
}
