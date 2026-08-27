using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Web;

/// <summary>
/// A news category, as the site's category filter shows it ("Campagnes", "Événements", "Jeux").
/// </summary>
/// <remarks>
/// The labels live in one JSON dictionary keyed by language code rather than in a translation table:
/// a whole table with its own fallback rules, for one string per language, would cost more to read
/// than it saves. The same fallback applies — a language absent from the dictionary reads the default
/// language's entry.
/// </remarks>
[Table("web_article_categories")]
[Index(nameof(Code), IsUnique = true)]
public class WebArticleCategoryEntity : VortexEntity
{
    /// <summary>The code the site puts in its URLs and in <c>?category=</c> ("campagnes"). Stable;
    /// renaming a label must never change it.</summary>
    [Column("code")]
    [MaxLength(64)]
    public required string Code { get; set; }

    /// <summary>Labels per language code, e.g. <c>{"fr":"Campagnes","en":"Campaigns"}</c>.</summary>
    [Column("label_json")]
    public required string LabelJson { get; set; }

    [Column("sort_order")]
    [DefaultValue(0)]
    public int SortOrder { get; set; }

    [Column("enabled")]
    [DefaultValue(true)]
    public bool Enabled { get; set; } = true;
}
