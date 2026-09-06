using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Vortex.Dashboard.API.Api;
using Vortex.Dashboard.API.Operations;
using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Hosting;

/// <summary>
/// The website's news: the article list and editor reads, and the writes that fill them. The public
/// side of the same tables is served by <c>Vortex.WebApi</c> and needs no session; this is the half
/// an operator uses, and it is the only way an article is ever written.
/// </summary>
internal static partial class DashboardEndpoints
{
    private const string TagArticles = "Articles";
    private const string ApiArticles = ApiV1 + "/articles";

    public static void MapArticleReads(WebApplication app)
    {
        MapReadGet(
            app,
            ApiArticles,
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.ArticlesAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.ArticlesRead,
            TagArticles
        );
        MapReadGet(
            app,
            ApiArticles + "/meta",
            (DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.ArticleFormMetaAsync(ct)),
            Capabilities.Dashboard.ArticlesRead,
            TagArticles
        );
        MapReadGet(
            app,
            ApiArticles + "/images",
            (HttpContext ctx, DashboardApiService api) =>
                Results.Ok(api.ArticleImages(ctx.QueryAsNameValues())),
            Capabilities.Dashboard.ArticlesRead,
            TagArticles
        );
        MapReadGet(
            app,
            ApiArticles + "/{articleId:int}",
            (int articleId, DashboardApiService api, CancellationToken ct) =>
                OkNullableAsync(api.ArticleDetailAsync(articleId, ct)),
            Capabilities.Dashboard.ArticlesRead,
            TagArticles
        );
    }

    public static void MapArticleOperations(WebApplication app)
    {
        MapPost(
            app,
            ApiOperations + "/articles",
            async (
                HttpContext ctx,
                ArticleRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                string.IsNullOrWhiteSpace(body.Slug) || string.IsNullOrWhiteSpace(body.Category)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.SaveArticleAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsArticlesManage,
            TagArticles
        );
        MapPost(
            app,
            ApiOperations + "/articles/delete",
            async (
                HttpContext ctx,
                DeleteArticleRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                Results.Ok(
                    await ops.DeleteArticleAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                ),
            Capabilities.Dashboard.OpsArticlesManage,
            TagArticles
        );
        MapPost(
            app,
            ApiOperations + "/articles/translation/delete",
            async (
                HttpContext ctx,
                DeleteArticleTranslationRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                Results.Ok(
                    await ops.DeleteArticleTranslationAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                ),
            Capabilities.Dashboard.OpsArticlesManage,
            TagArticles
        );
        MapPost(
            app,
            ApiOperations + "/articles/category",
            async (
                HttpContext ctx,
                ArticleCategoryRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                string.IsNullOrWhiteSpace(body.Code)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.SaveArticleCategoryAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsArticlesManage,
            TagArticles
        );
        MapPost(
            app,
            ApiOperations + "/articles/category/delete",
            async (
                HttpContext ctx,
                DeleteArticleCategoryRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                Results.Ok(
                    await ops.DeleteArticleCategoryAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                ),
            Capabilities.Dashboard.OpsArticlesManage,
            TagArticles
        );
        MapPost(
            app,
            ApiOperations + "/articles/language",
            async (
                HttpContext ctx,
                WebLanguageRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                string.IsNullOrWhiteSpace(body.Code) || string.IsNullOrWhiteSpace(body.Label)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.SaveWebLanguageAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsArticlesManage,
            TagArticles
        );
        MapPost(
            app,
            ApiOperations + "/articles/language/delete",
            async (
                HttpContext ctx,
                DeleteWebLanguageRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                Results.Ok(
                    await ops.DeleteWebLanguageAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                ),
            Capabilities.Dashboard.OpsArticlesManage,
            TagArticles
        );
    }
}
