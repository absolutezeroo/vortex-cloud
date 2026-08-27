using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Database.Entities.Web;
using Vortex.Primitives.Content;
using Vortex.WebApi.Services;
using Xunit;

namespace Vortex.WebApi.Tests;

/// <summary>
/// The rules that decide whether an article may be stored at all.
/// </summary>
/// <remarks>
/// This is the site's security boundary, and it is the whole of it: no markup crosses the body
/// column, so nothing downstream sanitises anything. A block type that slipped through, or an href
/// the browser would execute, would reach every visitor of the public site — and neither the build,
/// the dashboard nor the public read would notice.
/// </remarks>
public sealed class WebArticleAdminServiceTests
{
    private const string ValidBody = """[{"type":"p","text":"Bonjour"}]""";

    [Fact]
    public async Task Refuses_UnknownBlockType()
    {
        (WebArticleAdminService service, int articleId) = await SeededAsync();

        WebArticleAdminResult result = await SaveBodyAsync(
            service,
            articleId,
            """[{"type":"script","text":"x"}]"""
        );

        result.Error.Should().Be(WebArticleBody.ErrorBody);
    }

    [Fact]
    public async Task Refuses_JavascriptHref()
    {
        (WebArticleAdminService service, int articleId) = await SeededAsync();

        WebArticleAdminResult result = await SaveBodyAsync(
            service,
            articleId,
            """[{"type":"btn","label":"Clique","href":"javascript:alert(1)"}]"""
        );

        result.Error.Should().Be(WebArticleBody.ErrorHref);
    }

    [Fact]
    public async Task Refuses_ProtocolRelativeHref()
    {
        // "//evil.example/x" is a URL, not a path, and a browser follows it off-site.
        (WebArticleAdminService service, int articleId) = await SeededAsync();

        WebArticleAdminResult result = await SaveBodyAsync(
            service,
            articleId,
            """[{"type":"btn","label":"Clique","href":"//evil.example/x"}]"""
        );

        result.Error.Should().Be(WebArticleBody.ErrorHref);
    }

    [Fact]
    public async Task Refuses_TraversalInAnImageBlock()
    {
        (WebArticleAdminService service, int articleId) = await SeededAsync();

        WebArticleAdminResult result = await SaveBodyAsync(
            service,
            articleId,
            """[{"type":"img","src":"/../../secrets.png"}]"""
        );

        result.Error.Should().Be(WebArticleBody.ErrorImage);
    }

    [Fact]
    public async Task Refuses_AbsoluteHeaderImage()
    {
        (WebArticleAdminService service, int articleId) = await SeededAsync();

        WebArticleAdminResult result = await service.SaveTranslationAsync(
            articleId,
            "fr",
            new WebArticleTranslationSpec(
                "Titre",
                string.Empty,
                ValidBody,
                "http://evil.example/x.png",
                string.Empty
            ),
            CancellationToken.None
        );

        result.Error.Should().Be(WebArticleBody.ErrorImage);
    }

    [Fact]
    public async Task Accepts_AValidBody()
    {
        (WebArticleAdminService service, int articleId) = await SeededAsync();

        WebArticleAdminResult result = await SaveBodyAsync(
            service,
            articleId,
            """
            [{"type":"h","text":"Le programme"},
             {"type":"p","text":"Bonjour"},
             {"type":"img","src":"/web_promo/x.png","caption":"Le quartier"},
             {"type":"btn","label":"Au catalogue","href":"#/hotel"},
             {"type":"hr"}]
            """
        );

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Refuses_ASecondArticleWithTheSameSlug()
    {
        (WebArticleAdminService service, _) = await SeededAsync();

        WebArticleAdminResult result = await service.CreateArticleAsync(
            new WebArticleSpec(
                "abobbados",
                "campagnes",
                WebArticleStatusSpec.Draft,
                null,
                false,
                "Vortex"
            ),
            CancellationToken.None
        );

        result.Error.Should().Be(WebArticleAdminService.ErrorSlugTaken);
    }

    [Fact]
    public async Task Refuses_PublishedWithNoDate()
    {
        (WebArticleAdminService service, _) = await SeededAsync();

        WebArticleAdminResult result = await service.CreateArticleAsync(
            new WebArticleSpec(
                "sans-date",
                "campagnes",
                WebArticleStatusSpec.Published,
                null,
                false,
                "Vortex"
            ),
            CancellationToken.None
        );

        result.Error.Should().Be(WebArticleAdminService.ErrorPublishAt);
    }

    [Fact]
    public async Task Refuses_AnUppercaseSlug()
    {
        (WebArticleAdminService service, _) = await SeededAsync();

        WebArticleAdminResult result = await service.CreateArticleAsync(
            new WebArticleSpec(
                "Abobbados",
                "campagnes",
                WebArticleStatusSpec.Draft,
                null,
                false,
                "Vortex"
            ),
            CancellationToken.None
        );

        result.Error.Should().Be(WebArticleAdminService.ErrorSlug);
    }

    [Fact]
    public async Task Refuses_DeletingTheDefaultLanguage()
    {
        // Every read falls back to it. Without one the site serves nothing at all, which is a worse
        // outcome than any single language being missing.
        (WebArticleAdminService service, IDbContextFactory<VortexDbContext> factory) =
            await ServiceAsync();

        await SeedAsync(factory);

        await using VortexDbContext db = await factory.CreateDbContextAsync();
        int id = await db.WebLanguages.Where(l => l.IsDefault).Select(l => l.Id).FirstAsync();

        WebArticleAdminResult result = await service.DeleteLanguageAsync(
            id,
            CancellationToken.None
        );

        result.Error.Should().Be(WebArticleAdminService.ErrorLastLanguage);
    }

    [Fact]
    public async Task Refuses_DeletingACategoryThatStillHasArticles()
    {
        // The foreign key cascades: a hard delete here would take the articles with it.
        (WebArticleAdminService service, IDbContextFactory<VortexDbContext> factory) =
            await ServiceAsync();

        await SeedAsync(factory);

        await using VortexDbContext db = await factory.CreateDbContextAsync();
        int id = await db.WebArticleCategories.Select(c => c.Id).FirstAsync();

        WebArticleAdminResult result = await service.DeleteCategoryAsync(
            id,
            CancellationToken.None
        );

        result.Error.Should().Be(WebArticleAdminService.ErrorCategoryInUse);
    }

    [Fact]
    public async Task PromotingALanguage_DemotesThePreviousDefault()
    {
        (WebArticleAdminService service, IDbContextFactory<VortexDbContext> factory) =
            await ServiceAsync();

        await SeedAsync(factory);

        await using (VortexDbContext seed = await factory.CreateDbContextAsync())
        {
            seed.WebLanguages.Add(
                new WebLanguageEntity
                {
                    Code = "en",
                    Label = "English",
                    Enabled = true,
                }
            );

            await seed.SaveChangesAsync();
        }

        int englishId;

        await using (VortexDbContext read = await factory.CreateDbContextAsync())
        {
            englishId = await read
                .WebLanguages.Where(l => l.Code == "en")
                .Select(l => l.Id)
                .FirstAsync();
        }

        WebArticleAdminResult result = await service.SaveLanguageAsync(
            englishId,
            new WebLanguageSpec("en", "English", IsDefault: true, Enabled: true, SortOrder: 1),
            CancellationToken.None
        );

        result.Success.Should().BeTrue();

        await using VortexDbContext after = await factory.CreateDbContextAsync();

        // Exactly one, always: two defaults and the fallback becomes whichever row came back first.
        (await after.WebLanguages.CountAsync(l => l.IsDefault))
            .Should()
            .Be(1);
        (await after.WebLanguages.FirstAsync(l => l.IsDefault)).Code.Should().Be("en");
    }

    private static Task<WebArticleAdminResult> SaveBodyAsync(
        WebArticleAdminService service,
        int articleId,
        string body
    ) =>
        service.SaveTranslationAsync(
            articleId,
            "fr",
            new WebArticleTranslationSpec("Titre", string.Empty, body, string.Empty, string.Empty),
            CancellationToken.None
        );

    private static Task<(
        WebArticleAdminService Service,
        IDbContextFactory<VortexDbContext> Factory
    )> ServiceAsync()
    {
        TestDbContextFactory factory = new(
            new DbContextOptionsBuilder<VortexDbContext>()
                .UseInMemoryDatabase($"webapi-articles-{Guid.NewGuid():N}")
                .Options
        );

        return Task.FromResult<(WebArticleAdminService, IDbContextFactory<VortexDbContext>)>(
            (new WebArticleAdminService(factory), factory)
        );
    }

    private static async Task<(WebArticleAdminService Service, int ArticleId)> SeededAsync()
    {
        (WebArticleAdminService service, IDbContextFactory<VortexDbContext> factory) =
            await ServiceAsync();

        int articleId = await SeedAsync(factory);

        return (service, articleId);
    }

    private static async Task<int> SeedAsync(IDbContextFactory<VortexDbContext> factory)
    {
        await using VortexDbContext db = await factory.CreateDbContextAsync();

        db.WebLanguages.Add(
            new WebLanguageEntity
            {
                Code = "fr",
                Label = "Français",
                IsDefault = true,
                Enabled = true,
            }
        );

        WebArticleCategoryEntity category = new()
        {
            Code = "campagnes",
            LabelJson = """{"fr":"Campagnes"}""",
            Enabled = true,
        };

        db.WebArticleCategories.Add(category);
        await db.SaveChangesAsync();

        WebArticleEntity article = new()
        {
            Slug = "abobbados",
            CategoryId = category.Id,
            Status = WebArticleStatus.Draft,
            AuthorName = "Vortex",
        };

        db.WebArticles.Add(article);
        await db.SaveChangesAsync();

        return article.Id;
    }

    private sealed class TestDbContextFactory(DbContextOptions<VortexDbContext> options)
        : IDbContextFactory<VortexDbContext>
    {
        public VortexDbContext CreateDbContext() => new(options);
    }
}
