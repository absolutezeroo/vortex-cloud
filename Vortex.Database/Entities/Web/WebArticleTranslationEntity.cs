using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Web;

/// <summary>
/// One article as written in one language: everything a reader sees, images included.
/// </summary>
/// <remarks>
/// A missing translation is not an error — the read falls back to the default language and says so
/// (<c>fallback: true</c> in the response), so an article published in French shows up for an English
/// visitor rather than disappearing. An article with NO translation at all shows up nowhere.
/// </remarks>
[Table("web_article_translations")]
[Index(nameof(ArticleId), nameof(LanguageCode), IsUnique = true)]
public class WebArticleTranslationEntity : VortexEntity
{
    [Column("article_id")]
    public required int ArticleId { get; set; }

    [Column("language_code")]
    [MaxLength(8)]
    public required string LanguageCode { get; set; }

    [Column("title")]
    [MaxLength(255)]
    public required string Title { get; set; }

    /// <summary>The line under the title in the feed. Plain text — the feed renders it unstyled.</summary>
    [Column("summary")]
    [MaxLength(1024)]
    [DefaultValue("")]
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// The body as a JSON array of typed blocks — <c>p</c>, <c>h</c>, <c>img</c>, <c>btn</c>,
    /// <c>hr</c>. Never HTML: no markup crosses this column, so no sanitiser stands between the
    /// editor and the public site, and a cross-site script has nowhere to live. The write side
    /// validates the array against the closed block set before it is ever stored.
    /// </summary>
    /// <remarks>
    /// Explicitly <c>longtext</c>: <c>ModelBuilderExtensions</c> caps every unannotated string at
    /// varchar(512), which an article body reaches after a couple of paragraphs.
    /// </remarks>
    [Column("body_json", TypeName = "longtext")]
    public required string BodyJson { get; set; }

    /// <summary>Path under <c>c_images</c> ("/web_promo/Abobbados_largepromo.png") — the article's
    /// full-width picture. Relative because the site prefixes it with its own asset host.</summary>
    [Column("header_image")]
    [MaxLength(512)]
    [DefaultValue("")]
    public string HeaderImage { get; set; } = string.Empty;

    /// <summary>The 100x100 plate the feed shows. Falls back to <see cref="HeaderImage"/> when empty,
    /// which is what the site's <c>NewsList</c> already does.</summary>
    [Column("thumbnail")]
    [MaxLength(512)]
    [DefaultValue("")]
    public string Thumbnail { get; set; } = string.Empty;

    [ForeignKey(nameof(ArticleId))]
    public WebArticleEntity? Article { get; set; }
}
