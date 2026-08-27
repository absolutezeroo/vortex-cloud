using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Primitives.Content;

/// <summary>
/// Every write to the website's editorial tables. The dashboard's article page calls this and never
/// touches the tables itself, so the rules that make a body safe to serve — the closed block
/// vocabulary, the allowed link schemes, the image paths that stay inside <c>c_images</c> — are
/// enforced in one place rather than in whichever screen happens to be saving.
/// </summary>
/// <remarks>
/// There is no live cache and no grain behind these tables: an article is content, read straight from
/// the database by the public API. A write is therefore visible on the next request, with nothing to
/// invalidate and nothing that can drift.
/// </remarks>
public interface IWebArticleAdminService
{
    Task<WebArticleAdminResult> CreateArticleAsync(WebArticleSpec spec, CancellationToken ct);

    Task<WebArticleAdminResult> UpdateArticleAsync(
        int articleId,
        WebArticleSpec spec,
        CancellationToken ct
    );

    Task<WebArticleAdminResult> DeleteArticleAsync(int articleId, CancellationToken ct);

    /// <summary>Creates or replaces one language's text for an article.</summary>
    Task<WebArticleAdminResult> SaveTranslationAsync(
        int articleId,
        string languageCode,
        WebArticleTranslationSpec spec,
        CancellationToken ct
    );

    Task<WebArticleAdminResult> DeleteTranslationAsync(
        int articleId,
        string languageCode,
        CancellationToken ct
    );

    /// <summary>Creates when <paramref name="categoryId"/> is 0, updates otherwise.</summary>
    Task<WebArticleAdminResult> SaveCategoryAsync(
        int categoryId,
        WebArticleCategorySpec spec,
        CancellationToken ct
    );

    Task<WebArticleAdminResult> DeleteCategoryAsync(int categoryId, CancellationToken ct);

    /// <summary>Creates when <paramref name="languageId"/> is 0, updates otherwise.</summary>
    Task<WebArticleAdminResult> SaveLanguageAsync(
        int languageId,
        WebLanguageSpec spec,
        CancellationToken ct
    );

    Task<WebArticleAdminResult> DeleteLanguageAsync(int languageId, CancellationToken ct);
}
