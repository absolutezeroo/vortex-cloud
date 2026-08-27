using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Database.Entities.Web;
using Xunit;

namespace Vortex.WebApi.Tests;

/// <summary>
/// The website's editorial reads, exercised through the real routing pipeline against a seeded
/// database.
/// </summary>
/// <remarks>
/// The cases here are the ones a reader cannot see and therefore nobody would notice breaking: a
/// draft leaking into the feed, an article scheduled for next week appearing today, a language with
/// no translation serving nothing instead of falling back. Each of those is invisible until it is
/// wrong on the live site.
/// </remarks>
public sealed class ArticleEndpointsTests
{
    private const string Body = """[{"type":"p","text":"Hello"}]""";

    [Fact]
    public async Task Feed_HidesDrafts()
    {
        await using WebApiTestFactory factory = new();
        await SeedAsync(factory, ("live", WebArticleStatus.Published, Hours(-1)));
        await SeedAsync(factory, ("hidden", WebArticleStatus.Draft, Hours(-1)));

        JsonElement feed = await GetJsonAsync(factory.Client, "/api/public/articles");

        Slugs(feed).Should().ContainSingle().Which.Should().Be("live");
    }

    [Fact]
    public async Task Feed_HidesArticlesScheduledForLater()
    {
        await using WebApiTestFactory factory = new();
        await SeedAsync(factory, ("live", WebArticleStatus.Published, Hours(-1)));
        await SeedAsync(factory, ("tomorrow", WebArticleStatus.Published, Hours(24)));

        JsonElement feed = await GetJsonAsync(factory.Client, "/api/public/articles");

        Slugs(feed).Should().ContainSingle().Which.Should().Be("live");
    }

    [Fact]
    public async Task Feed_PutsPinnedAheadOfMoreRecent()
    {
        await using WebApiTestFactory factory = new();
        await SeedAsync(
            factory,
            ("older-pinned", WebArticleStatus.Published, Hours(-48)),
            pinned: true
        );
        await SeedAsync(factory, ("newer", WebArticleStatus.Published, Hours(-1)));

        JsonElement feed = await GetJsonAsync(factory.Client, "/api/public/articles");

        Slugs(feed).Should().Equal("older-pinned", "newer");
    }

    [Fact]
    public async Task Feed_PagesWithoutOverlap()
    {
        await using WebApiTestFactory factory = new();

        for (int i = 0; i < 5; i++)
        {
            await SeedAsync(factory, ($"article-{i}", WebArticleStatus.Published, Hours(-i - 1)));
        }

        JsonElement first = await GetJsonAsync(
            factory.Client,
            "/api/public/articles?page=1&pageSize=2"
        );
        JsonElement second = await GetJsonAsync(
            factory.Client,
            "/api/public/articles?page=2&pageSize=2"
        );

        first.GetProperty("total").GetInt32().Should().Be(5);
        Slugs(first).Should().HaveCount(2);
        Slugs(second).Should().HaveCount(2);
        Slugs(first).Should().NotIntersectWith(Slugs(second));
    }

    [Fact]
    public async Task Feed_HidesDeletedArticles()
    {
        // DeletedAt comes from VortexEntity and nothing filters it automatically, so the one thing
        // worth asserting is that this query does.
        await using WebApiTestFactory factory = new();
        await SeedAsync(factory, ("gone", WebArticleStatus.Published, Hours(-1)), deleted: true);

        JsonElement feed = await GetJsonAsync(factory.Client, "/api/public/articles");

        Slugs(feed).Should().BeEmpty();
    }

    [Fact]
    public async Task Feed_HidesArticlesWithNoTranslation()
    {
        await using WebApiTestFactory factory = new();
        await SeedAsync(
            factory,
            ("untranslated", WebArticleStatus.Published, Hours(-1)),
            translate: false
        );

        JsonElement feed = await GetJsonAsync(factory.Client, "/api/public/articles");

        Slugs(feed).Should().BeEmpty();
    }

    [Fact]
    public async Task Article_FallsBackToTheDefaultLanguageAndSaysSo()
    {
        await using WebApiTestFactory factory = new();
        await SeedAsync(factory, ("abobbados", WebArticleStatus.Published, Hours(-1)));

        JsonElement article = await GetJsonAsync(
            factory.Client,
            "/api/public/articles/abobbados?lang=en"
        );

        article.GetProperty("lang").GetString().Should().Be("en");
        article.GetProperty("article").GetProperty("fallback").GetBoolean().Should().BeTrue();
        article.GetProperty("article").GetProperty("title").GetString().Should().Be("abobbados fr");
    }

    [Fact]
    public async Task Article_ServesTheRequestedLanguageWhenItExists()
    {
        await using WebApiTestFactory factory = new();
        await SeedAsync(factory, ("abobbados", WebArticleStatus.Published, Hours(-1)));

        await using (VortexDbContext db = await factory.DbContexts.CreateDbContextAsync())
        {
            int id = await db.WebArticles.Select(a => a.Id).FirstAsync();

            db.WebArticleTranslations.Add(
                new WebArticleTranslationEntity
                {
                    ArticleId = id,
                    LanguageCode = "en",
                    Title = "abobbados en",
                    BodyJson = Body,
                }
            );

            await db.SaveChangesAsync();
        }

        JsonElement article = await GetJsonAsync(
            factory.Client,
            "/api/public/articles/abobbados?lang=en"
        );

        article.GetProperty("article").GetProperty("fallback").GetBoolean().Should().BeFalse();
        article.GetProperty("article").GetProperty("title").GetString().Should().Be("abobbados en");
    }

    [Fact]
    public async Task Feed_TreatsAnUnknownLanguageAsNoPreference()
    {
        // A browser asking for a language the hotel does not publish must still get the site.
        await using WebApiTestFactory factory = new();
        await SeedAsync(factory, ("live", WebArticleStatus.Published, Hours(-1)));

        HttpResponseMessage response = await factory.Client.GetAsync(
            "/api/public/articles?lang=zz"
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        JsonElement feed = await ReadJsonAsync(response);
        feed.GetProperty("lang").GetString().Should().Be("fr");
        Slugs(feed).Should().ContainSingle();
    }

    [Fact]
    public async Task Article_UnknownSlugIs404()
    {
        await using WebApiTestFactory factory = new();

        HttpResponseMessage response = await factory.Client.GetAsync(
            "/api/public/articles/nothing-here"
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Article_DraftIs404EvenByDirectUrl()
    {
        await using WebApiTestFactory factory = new();
        await SeedAsync(factory, ("secret", WebArticleStatus.Draft, Hours(-1)));

        HttpResponseMessage response = await factory.Client.GetAsync("/api/public/articles/secret");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Article_ArchivedStaysReadable()
    {
        // Out of the feed, but any link ever shared still resolves — which is the point of archiving
        // rather than deleting.
        await using WebApiTestFactory factory = new();
        await SeedAsync(factory, ("old-news", WebArticleStatus.Archived, Hours(-500)));

        JsonElement feed = await GetJsonAsync(factory.Client, "/api/public/articles");
        Slugs(feed).Should().BeEmpty();

        HttpResponseMessage response = await factory.Client.GetAsync(
            "/api/public/articles/old-news"
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Languages_ReportsTheDefault()
    {
        await using WebApiTestFactory factory = new();
        await SeedAsync(factory, ("live", WebArticleStatus.Published, Hours(-1)));

        JsonElement languages = await GetJsonAsync(factory.Client, "/api/public/languages");

        languages.GetProperty("default").GetString().Should().Be("fr");
        languages.GetProperty("items").GetArrayLength().Should().Be(2);
    }

    private static DateTime Hours(int offset) => DateTime.UtcNow.AddHours(offset);

    private static async Task SeedAsync(
        WebApiTestFactory factory,
        (string Slug, WebArticleStatus Status, DateTime PublishAt) article,
        bool pinned = false,
        bool deleted = false,
        bool translate = true
    )
    {
        await using VortexDbContext db = await factory.DbContexts.CreateDbContextAsync();

        if (!await db.WebLanguages.AnyAsync())
        {
            // Two languages, one of them the default: the fallback rule only has anything to say
            // when the hotel actually publishes a second language.
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
        }

        int categoryId = await db.WebArticleCategories.Select(c => c.Id).FirstAsync();

        WebArticleEntity entity = new()
        {
            Slug = article.Slug,
            CategoryId = categoryId,
            Status = article.Status,
            PublishAt = article.PublishAt,
            Pinned = pinned,
            AuthorName = "Vortex",
            DeletedAt = deleted ? DateTime.UtcNow : null,
        };

        db.WebArticles.Add(entity);
        await db.SaveChangesAsync();

        if (translate)
        {
            db.WebArticleTranslations.Add(
                new WebArticleTranslationEntity
                {
                    ArticleId = entity.Id,
                    LanguageCode = "fr",
                    Title = $"{article.Slug} fr",
                    Summary = "résumé",
                    BodyJson = Body,
                    HeaderImage = "/web_promo/x.png",
                }
            );

            await db.SaveChangesAsync();
        }
    }

    private static async Task<JsonElement> GetJsonAsync(HttpClient client, string url) =>
        await ReadJsonAsync(await client.GetAsync(url));

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();

        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }

    private static string[] Slugs(JsonElement feed)
    {
        JsonElement items = feed.GetProperty("items");
        string[] slugs = new string[items.GetArrayLength()];
        int index = 0;

        foreach (JsonElement item in items.EnumerateArray())
        {
            slugs[index++] = item.GetProperty("id").GetString()!;
        }

        return slugs;
    }
}
