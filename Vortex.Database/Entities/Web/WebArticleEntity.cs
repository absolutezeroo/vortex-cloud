using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Web;

/// <summary>
/// Where an article stands. There is deliberately no <c>Scheduled</c> value: an article waiting for
/// its date is <see cref="Published"/> with a <see cref="WebArticleEntity.PublishAt"/> in the future,
/// and the public read filters it out on the date alone. That keeps the whole scheduling story inside
/// one WHERE clause — no background service, no timer, and nothing to catch up on after a restart.
/// </summary>
public enum WebArticleStatus
{
    /// <summary>Never public, whatever the date says.</summary>
    Draft = 0,

    /// <summary>Public once <see cref="WebArticleEntity.PublishAt"/> has passed.</summary>
    Published = 1,

    /// <summary>Out of the feed, but its URL still reads — habbo.com keeps old articles reachable.</summary>
    Archived = 2,
}

/// <summary>
/// One news article. This row carries only what is EDITORIAL — when it comes out, where it is filed,
/// whether it leads the page. Everything WRITTEN (title, summary, body, images) lives on
/// <see cref="WebArticleTranslationEntity"/>, one row per language.
/// </summary>
/// <remarks>
/// The header image is on the translation and not here on purpose: 168 of the 3 284 files under
/// <c>c_images/web_promo</c> are language variants (<c>Schreibwerkstatt_DE_LargePromo.png</c>,
/// <c>WebPromo_FanSites_FR.png</c>), so a shared image would put French promo art on top of a German
/// article.
/// <para>
/// The slug is shared across languages, which is what keeps the share URL
/// (<c>GET /article/{slug}</c>, the one carrying the Open Graph tags) single.
/// </para>
/// </remarks>
[Table("web_articles")]
[Index(nameof(Slug), IsUnique = true)]
[Index(nameof(Status), nameof(PublishAt))]
public class WebArticleEntity : VortexEntity
{
    /// <summary>Lowercase, digits and dashes. It is the article's public id — the site's feed sends it
    /// as <c>id</c> and its URL is <c>/article/{slug}</c>.</summary>
    [Column("slug")]
    [MaxLength(128)]
    public required string Slug { get; set; }

    [Column("category_id")]
    public required int CategoryId { get; set; }

    [Column("status")]
    [DefaultValue(WebArticleStatus.Draft)]
    public WebArticleStatus Status { get; set; } = WebArticleStatus.Draft;

    /// <summary>When the article becomes public. Required as soon as the status is
    /// <see cref="WebArticleStatus.Published"/>; a future value is what "scheduled" means here.</summary>
    [Column("publish_at")]
    public DateTime? PublishAt { get; set; }

    /// <summary>Leads the feed, ahead of more recent articles.</summary>
    [Column("pinned")]
    [DefaultValue(false)]
    public bool Pinned { get; set; }

    /// <summary>The byline as the site prints it. Free text rather than a foreign key: a hotel signs
    /// articles "Vortex" or "L'équipe" as often as with a person's name, and an author table would
    /// force one of those to be a fake account.</summary>
    [Column("author_name")]
    [MaxLength(64)]
    [DefaultValue("")]
    public string AuthorName { get; set; } = string.Empty;

    [ForeignKey(nameof(CategoryId))]
    public WebArticleCategoryEntity? Category { get; set; }
}
