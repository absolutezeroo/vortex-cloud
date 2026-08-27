using System;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Content;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// The website's news, written. Every one of these routes through
/// <see cref="IWebArticleAdminService"/> — never a direct write — so the rules that keep a body safe
/// to serve are enforced once, wherever the save came from, and each is audited with its operator's
/// reason like the rest of the dashboard.
/// </summary>
internal sealed partial class DashboardOperationsService
{
    public Task<OperationResult> SaveArticleAsync(
        ArticleRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            request.ArticleId > 0 ? "ops.article.update" : "ops.article.create",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.ArticleId,
                request.Slug,
                request.Category,
                request.Status,
            },
            work: async c =>
            {
                WebArticleSpec spec = new(
                    request.Slug,
                    request.Category,
                    ParseStatus(request.Status),
                    request.PublishAt,
                    request.Pinned,
                    request.Author
                );

                WebArticleAdminResult saved =
                    request.ArticleId > 0
                        ? await _webArticleAdmin
                            .UpdateArticleAsync(request.ArticleId, spec, c)
                            .ConfigureAwait(false)
                        : await _webArticleAdmin.CreateArticleAsync(spec, c).ConfigureAwait(false);

                Throw(saved);

                if (request.Translation is not { } translation)
                {
                    return;
                }

                // Same operation, so a rejected body takes the whole save down rather than leaving
                // an article whose text silently did not land.
                Throw(
                    await _webArticleAdmin
                        .SaveTranslationAsync(
                            saved.Id,
                            translation.Lang,
                            new WebArticleTranslationSpec(
                                translation.Title,
                                translation.Summary,
                                translation.Body,
                                translation.HeaderImage,
                                translation.Thumbnail
                            ),
                            c
                        )
                        .ConfigureAwait(false)
                );
            },
            ct
        );

    public Task<OperationResult> DeleteArticleAsync(
        DeleteArticleRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.article.delete",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.ArticleId },
            work: async c =>
                Throw(
                    await _webArticleAdmin
                        .DeleteArticleAsync(request.ArticleId, c)
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> DeleteArticleTranslationAsync(
        DeleteArticleTranslationRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.article.translation.delete",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.ArticleId, request.Lang },
            work: async c =>
                Throw(
                    await _webArticleAdmin
                        .DeleteTranslationAsync(request.ArticleId, request.Lang, c)
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> SaveArticleCategoryAsync(
        ArticleCategoryRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            request.CategoryId > 0 ? "ops.article.category.update" : "ops.article.category.create",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.CategoryId, request.Code },
            work: async c =>
                Throw(
                    await _webArticleAdmin
                        .SaveCategoryAsync(
                            request.CategoryId,
                            new WebArticleCategorySpec(
                                request.Code,
                                request.Labels,
                                request.SortOrder,
                                request.Enabled
                            ),
                            c
                        )
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> DeleteArticleCategoryAsync(
        DeleteArticleCategoryRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.article.category.delete",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.CategoryId },
            work: async c =>
                Throw(
                    await _webArticleAdmin
                        .DeleteCategoryAsync(request.CategoryId, c)
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> SaveWebLanguageAsync(
        WebLanguageRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            request.LanguageId > 0 ? "ops.article.language.update" : "ops.article.language.create",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.LanguageId,
                request.Code,
                request.IsDefault,
            },
            work: async c =>
                Throw(
                    await _webArticleAdmin
                        .SaveLanguageAsync(
                            request.LanguageId,
                            new WebLanguageSpec(
                                request.Code,
                                request.Label,
                                request.IsDefault,
                                request.Enabled,
                                request.SortOrder
                            ),
                            c
                        )
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> DeleteWebLanguageAsync(
        DeleteWebLanguageRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.article.language.delete",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.LanguageId },
            work: async c =>
                Throw(
                    await _webArticleAdmin
                        .DeleteLanguageAsync(request.LanguageId, c)
                        .ConfigureAwait(false)
                ),
            ct
        );

    /// <summary>An unknown status is a draft, never a publication: a typo in a request body must not
    /// be able to put an article on the front page.</summary>
    private static WebArticleStatusSpec ParseStatus(string? status) =>
        Enum.TryParse(status, ignoreCase: true, out WebArticleStatusSpec parsed)
            ? parsed
            : WebArticleStatusSpec.Draft;

    private static void Throw(WebArticleAdminResult result)
    {
        if (!result.Success)
        {
            throw new InvalidOperationException(result.Error);
        }
    }
}
