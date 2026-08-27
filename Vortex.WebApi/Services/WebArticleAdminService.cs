using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Database.Entities.Web;
using Vortex.Primitives.Content;

namespace Vortex.WebApi.Services;

/// <summary>
/// Every write to the website's editorial tables, and the only place that decides whether one is
/// allowed. It lives beside the public read rather than in the dashboard because both halves are the
/// website's: the screen that edits an article is one caller, and a second one must not be able to
/// arrive with its own idea of what a valid body is.
/// </summary>
public sealed class WebArticleAdminService(IDbContextFactory<VortexDbContext> dbCtxFactory)
    : IWebArticleAdminService
{
    internal const string ErrorSlug = "invalid_slug";
    internal const string ErrorSlugTaken = "slug_taken";
    internal const string ErrorTitle = "title_required";
    internal const string ErrorPublishAt = "publish_at_required";
    internal const string ErrorArticleNotFound = "article_not_found";
    internal const string ErrorCategoryNotFound = "category_not_found";
    internal const string ErrorCategoryInUse = "category_in_use";
    internal const string ErrorLanguageNotFound = "language_not_found";
    internal const string ErrorLanguageCode = "invalid_language_code";
    internal const string ErrorLastLanguage = "last_language_protected";
    internal const string ErrorTranslationNotFound = "translation_not_found";
    internal const string ErrorLabel = "invalid_label";

    private const int MAX_SLUG_LENGTH = 128;
    private const int MAX_LANGUAGE_CODE_LENGTH = 8;

    private readonly IDbContextFactory<VortexDbContext> _db = dbCtxFactory;

    public async Task<WebArticleAdminResult> CreateArticleAsync(
        WebArticleSpec spec,
        CancellationToken ct
    )
    {
        await using VortexDbContext db = await _db.CreateDbContextAsync(ct).ConfigureAwait(false);

        if (Validate(spec) is { } failure)
        {
            return WebArticleAdminResult.Fail(failure);
        }

        if (await db.WebArticles.AnyAsync(a => a.Slug == spec.Slug, ct).ConfigureAwait(false))
        {
            // Deleted articles keep their slug: reusing one would make an old shared link resolve to
            // a different article, silently.
            return WebArticleAdminResult.Fail(ErrorSlugTaken);
        }

        WebArticleCategoryEntity? category = await FindCategoryAsync(db, spec.CategoryCode, ct)
            .ConfigureAwait(false);

        if (category is null)
        {
            return WebArticleAdminResult.Fail(ErrorCategoryNotFound);
        }

        WebArticleEntity article = new()
        {
            Slug = spec.Slug,
            CategoryId = category.Id,
            Status = (WebArticleStatus)spec.Status,
            PublishAt = spec.PublishAt,
            Pinned = spec.Pinned,
            AuthorName = spec.AuthorName ?? string.Empty,
        };

        db.WebArticles.Add(article);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return WebArticleAdminResult.Ok(article.Id);
    }

    public async Task<WebArticleAdminResult> UpdateArticleAsync(
        int articleId,
        WebArticleSpec spec,
        CancellationToken ct
    )
    {
        await using VortexDbContext db = await _db.CreateDbContextAsync(ct).ConfigureAwait(false);

        if (Validate(spec) is { } failure)
        {
            return WebArticleAdminResult.Fail(failure);
        }

        WebArticleEntity? article = await db
            .WebArticles.FirstOrDefaultAsync(a => a.Id == articleId && a.DeletedAt == null, ct)
            .ConfigureAwait(false);

        if (article is null)
        {
            return WebArticleAdminResult.Fail(ErrorArticleNotFound);
        }

        if (
            await db
                .WebArticles.AnyAsync(a => a.Slug == spec.Slug && a.Id != articleId, ct)
                .ConfigureAwait(false)
        )
        {
            return WebArticleAdminResult.Fail(ErrorSlugTaken);
        }

        WebArticleCategoryEntity? category = await FindCategoryAsync(db, spec.CategoryCode, ct)
            .ConfigureAwait(false);

        if (category is null)
        {
            return WebArticleAdminResult.Fail(ErrorCategoryNotFound);
        }

        article.Slug = spec.Slug;
        article.CategoryId = category.Id;
        article.Status = (WebArticleStatus)spec.Status;
        article.PublishAt = spec.PublishAt;
        article.Pinned = spec.Pinned;
        article.AuthorName = spec.AuthorName ?? string.Empty;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return WebArticleAdminResult.Ok(article.Id);
    }

    public async Task<WebArticleAdminResult> DeleteArticleAsync(int articleId, CancellationToken ct)
    {
        await using VortexDbContext db = await _db.CreateDbContextAsync(ct).ConfigureAwait(false);

        WebArticleEntity? article = await db
            .WebArticles.FirstOrDefaultAsync(a => a.Id == articleId && a.DeletedAt == null, ct)
            .ConfigureAwait(false);

        if (article is null)
        {
            return WebArticleAdminResult.Fail(ErrorArticleNotFound);
        }

        // Soft: the audit interceptor captures what the row held, and an article deleted by mistake
        // is one UPDATE away from coming back. The public reads all filter DeletedAt, so it is gone
        // from the site the moment this returns.
        article.DeletedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return WebArticleAdminResult.Ok(articleId);
    }

    public async Task<WebArticleAdminResult> SaveTranslationAsync(
        int articleId,
        string languageCode,
        WebArticleTranslationSpec spec,
        CancellationToken ct
    )
    {
        await using VortexDbContext db = await _db.CreateDbContextAsync(ct).ConfigureAwait(false);

        if (!IsLanguageCode(languageCode))
        {
            return WebArticleAdminResult.Fail(ErrorLanguageCode);
        }

        if (string.IsNullOrWhiteSpace(spec.Title))
        {
            return WebArticleAdminResult.Fail(ErrorTitle);
        }

        if (!WebArticleBody.TryValidate(spec.BodyJson, out string bodyError))
        {
            return WebArticleAdminResult.Fail(bodyError);
        }

        if (
            !WebArticleBody.IsAllowedImagePath(spec.HeaderImage)
            || !WebArticleBody.IsAllowedImagePath(spec.Thumbnail)
        )
        {
            return WebArticleAdminResult.Fail(WebArticleBody.ErrorImage);
        }

        if (
            !await db
                .WebArticles.AnyAsync(a => a.Id == articleId && a.DeletedAt == null, ct)
                .ConfigureAwait(false)
        )
        {
            return WebArticleAdminResult.Fail(ErrorArticleNotFound);
        }

        if (
            !await db
                .WebLanguages.AnyAsync(l => l.Code == languageCode && l.DeletedAt == null, ct)
                .ConfigureAwait(false)
        )
        {
            return WebArticleAdminResult.Fail(ErrorLanguageNotFound);
        }

        WebArticleTranslationEntity? translation = await db
            .WebArticleTranslations.FirstOrDefaultAsync(
                t => t.ArticleId == articleId && t.LanguageCode == languageCode,
                ct
            )
            .ConfigureAwait(false);

        if (translation is null)
        {
            translation = new WebArticleTranslationEntity
            {
                ArticleId = articleId,
                LanguageCode = languageCode,
                Title = spec.Title,
                BodyJson = spec.BodyJson,
            };

            db.WebArticleTranslations.Add(translation);
        }

        translation.Title = spec.Title;
        translation.Summary = spec.Summary ?? string.Empty;
        translation.BodyJson = spec.BodyJson;
        translation.HeaderImage = spec.HeaderImage ?? string.Empty;
        translation.Thumbnail = spec.Thumbnail ?? string.Empty;

        // Saving a language that had been removed brings it back rather than colliding with the
        // unique index on (article_id, language_code).
        translation.DeletedAt = null;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return WebArticleAdminResult.Ok(translation.Id);
    }

    public async Task<WebArticleAdminResult> DeleteTranslationAsync(
        int articleId,
        string languageCode,
        CancellationToken ct
    )
    {
        await using VortexDbContext db = await _db.CreateDbContextAsync(ct).ConfigureAwait(false);

        WebArticleTranslationEntity? translation = await db
            .WebArticleTranslations.FirstOrDefaultAsync(
                t =>
                    t.ArticleId == articleId
                    && t.LanguageCode == languageCode
                    && t.DeletedAt == null,
                ct
            )
            .ConfigureAwait(false);

        if (translation is null)
        {
            return WebArticleAdminResult.Fail(ErrorTranslationNotFound);
        }

        translation.DeletedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return WebArticleAdminResult.Ok(translation.Id);
    }

    public async Task<WebArticleAdminResult> SaveCategoryAsync(
        int categoryId,
        WebArticleCategorySpec spec,
        CancellationToken ct
    )
    {
        await using VortexDbContext db = await _db.CreateDbContextAsync(ct).ConfigureAwait(false);

        if (!IsSlug(spec.Code))
        {
            return WebArticleAdminResult.Fail(ErrorSlug);
        }

        if (!IsLabelDictionary(spec.LabelJson))
        {
            return WebArticleAdminResult.Fail(ErrorLabel);
        }

        WebArticleCategoryEntity? category =
            categoryId > 0
                ? await db
                    .WebArticleCategories.FirstOrDefaultAsync(
                        c => c.Id == categoryId && c.DeletedAt == null,
                        ct
                    )
                    .ConfigureAwait(false)
                : null;

        if (categoryId > 0 && category is null)
        {
            return WebArticleAdminResult.Fail(ErrorCategoryNotFound);
        }

        if (
            await db
                .WebArticleCategories.AnyAsync(c => c.Code == spec.Code && c.Id != categoryId, ct)
                .ConfigureAwait(false)
        )
        {
            return WebArticleAdminResult.Fail(ErrorSlugTaken);
        }

        if (category is null)
        {
            category = new WebArticleCategoryEntity
            {
                Code = spec.Code,
                LabelJson = spec.LabelJson,
            };
            db.WebArticleCategories.Add(category);
        }

        category.Code = spec.Code;
        category.LabelJson = spec.LabelJson;
        category.SortOrder = spec.SortOrder;
        category.Enabled = spec.Enabled;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return WebArticleAdminResult.Ok(category.Id);
    }

    public async Task<WebArticleAdminResult> DeleteCategoryAsync(
        int categoryId,
        CancellationToken ct
    )
    {
        await using VortexDbContext db = await _db.CreateDbContextAsync(ct).ConfigureAwait(false);

        WebArticleCategoryEntity? category = await db
            .WebArticleCategories.FirstOrDefaultAsync(
                c => c.Id == categoryId && c.DeletedAt == null,
                ct
            )
            .ConfigureAwait(false);

        if (category is null)
        {
            return WebArticleAdminResult.Fail(ErrorCategoryNotFound);
        }

        // The foreign key cascades, so a category that still has articles must be refused here: a
        // hard delete would take them with it, and a soft one would leave them filed under a category
        // that no longer resolves.
        if (
            await db
                .WebArticles.AnyAsync(a => a.CategoryId == categoryId && a.DeletedAt == null, ct)
                .ConfigureAwait(false)
        )
        {
            return WebArticleAdminResult.Fail(ErrorCategoryInUse);
        }

        category.DeletedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return WebArticleAdminResult.Ok(categoryId);
    }

    public async Task<WebArticleAdminResult> SaveLanguageAsync(
        int languageId,
        WebLanguageSpec spec,
        CancellationToken ct
    )
    {
        await using VortexDbContext db = await _db.CreateDbContextAsync(ct).ConfigureAwait(false);

        if (!IsLanguageCode(spec.Code) || string.IsNullOrWhiteSpace(spec.Label))
        {
            return WebArticleAdminResult.Fail(ErrorLanguageCode);
        }

        WebLanguageEntity? language =
            languageId > 0
                ? await db
                    .WebLanguages.FirstOrDefaultAsync(
                        l => l.Id == languageId && l.DeletedAt == null,
                        ct
                    )
                    .ConfigureAwait(false)
                : null;

        if (languageId > 0 && language is null)
        {
            return WebArticleAdminResult.Fail(ErrorLanguageNotFound);
        }

        if (
            await db
                .WebLanguages.AnyAsync(l => l.Code == spec.Code && l.Id != languageId, ct)
                .ConfigureAwait(false)
        )
        {
            return WebArticleAdminResult.Fail(ErrorSlugTaken);
        }

        // Disabling or demoting the only usable language leaves every read with no fallback to fall
        // back to, which is a site that serves nothing rather than a site in one language.
        if (language is not null && language.IsDefault && (!spec.IsDefault || !spec.Enabled))
        {
            bool anotherDefaultRemains = await db
                .WebLanguages.AnyAsync(
                    l => l.Id != languageId && l.Enabled && l.DeletedAt == null,
                    ct
                )
                .ConfigureAwait(false);

            if (!anotherDefaultRemains)
            {
                return WebArticleAdminResult.Fail(ErrorLastLanguage);
            }
        }

        if (language is null)
        {
            language = new WebLanguageEntity { Code = spec.Code, Label = spec.Label };
            db.WebLanguages.Add(language);
        }

        // Somebody has to be the fallback. When no row claims it — the first language a hotel opens,
        // or a table left without one — this is it, whatever the form said: a site whose every read
        // falls back to nothing serves nothing.
        bool anyDefault = await db
            .WebLanguages.AnyAsync(
                l => l.IsDefault && l.Id != language.Id && l.DeletedAt == null,
                ct
            )
            .ConfigureAwait(false);

        language.Code = spec.Code;
        language.Label = spec.Label;
        language.IsDefault = spec.IsDefault || !anyDefault;
        language.Enabled = spec.Enabled;
        language.SortOrder = spec.SortOrder;

        if (language.IsDefault)
        {
            // Exactly one default, always: promoting one demotes the rest in the same transaction so
            // there is never a moment with two, or with none.
            List<WebLanguageEntity> demoted = await db
                .WebLanguages.Where(l => l.Id != language.Id && l.IsDefault)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            foreach (WebLanguageEntity other in demoted)
            {
                other.IsDefault = false;
            }
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return WebArticleAdminResult.Ok(language.Id);
    }

    public async Task<WebArticleAdminResult> DeleteLanguageAsync(
        int languageId,
        CancellationToken ct
    )
    {
        await using VortexDbContext db = await _db.CreateDbContextAsync(ct).ConfigureAwait(false);

        WebLanguageEntity? language = await db
            .WebLanguages.FirstOrDefaultAsync(l => l.Id == languageId && l.DeletedAt == null, ct)
            .ConfigureAwait(false);

        if (language is null)
        {
            return WebArticleAdminResult.Fail(ErrorLanguageNotFound);
        }

        bool anotherRemains = await db
            .WebLanguages.AnyAsync(l => l.Id != languageId && l.Enabled && l.DeletedAt == null, ct)
            .ConfigureAwait(false);

        if (language.IsDefault || !anotherRemains)
        {
            return WebArticleAdminResult.Fail(ErrorLastLanguage);
        }

        language.DeletedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return WebArticleAdminResult.Ok(languageId);
    }

    /// <summary>The editorial rules that do not need the database. Null means the spec is acceptable.</summary>
    private static string? Validate(WebArticleSpec spec)
    {
        if (!IsSlug(spec.Slug))
        {
            return ErrorSlug;
        }

        // Published with no date would be an article that is live and undated: the feed orders on
        // this column and the site prints it, so there is nowhere for a missing value to hide.
        if (spec.Status == WebArticleStatusSpec.Published && spec.PublishAt is null)
        {
            return ErrorPublishAt;
        }

        return null;
    }

    private static Task<WebArticleCategoryEntity?> FindCategoryAsync(
        VortexDbContext db,
        string code,
        CancellationToken ct
    ) =>
        db.WebArticleCategories.FirstOrDefaultAsync(c => c.Code == code && c.DeletedAt == null, ct);

    /// <summary>
    /// Lowercase letters, digits and single dashes. A slug is half of a public URL and the whole of
    /// an article's identity in the feed, so it is kept to what survives a copy-paste, a mail client
    /// and a Discord embed unchanged.
    /// </summary>
    private static bool IsSlug(string? value)
    {
        if (
            string.IsNullOrEmpty(value)
            || value.Length > MAX_SLUG_LENGTH
            || value[0] == '-'
            || value[^1] == '-'
        )
        {
            return false;
        }

        foreach (char c in value)
        {
            if (!(char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-'))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsLanguageCode(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length is < 2 or > MAX_LANGUAGE_CODE_LENGTH)
        {
            return false;
        }

        foreach (char c in value)
        {
            if (!(char.IsAsciiLetterLower(c) || c == '-'))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// A category's labels must be a flat <c>{"fr":"…"}</c> object. Anything else would read back as
    /// the category's own code on the site, which looks like a missing translation rather than a
    /// malformed row — so it is refused at the door.
    /// </summary>
    private static bool IsLabelDictionary(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            foreach (JsonProperty property in document.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.String)
                {
                    return false;
                }
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
