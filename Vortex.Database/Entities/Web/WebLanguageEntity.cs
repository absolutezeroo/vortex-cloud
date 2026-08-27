using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Web;

/// <summary>
/// A language the website is published in. This is a table and not a configuration option because
/// opening a language is business data an operator changes: it must not need a rebuild or a restart,
/// same reasoning as <c>currency_types</c>.
/// </summary>
/// <remarks>
/// Exactly one row carries <see cref="IsDefault"/>. It is the fallback every read falls back TO when
/// the requested language has no translation, so the write side refuses to delete it or to disable
/// the last enabled language — a site with no default language can serve nothing at all.
/// </remarks>
[Table("web_languages")]
[Index(nameof(Code), IsUnique = true)]
public class WebLanguageEntity : VortexEntity
{
    /// <summary>ISO-ish short code as the site uses it in <c>?lang=</c> and in its locale file name
    /// (<c>fr</c>, <c>en</c>).</summary>
    [Column("code")]
    [MaxLength(8)]
    public required string Code { get; set; }

    /// <summary>The language's name in itself ("Français", "English") — that is how a language picker
    /// is expected to read, never translated into the current language.</summary>
    [Column("label")]
    [MaxLength(64)]
    public required string Label { get; set; }

    [Column("is_default")]
    [DefaultValue(false)]
    public bool IsDefault { get; set; }

    [Column("enabled")]
    [DefaultValue(true)]
    public bool Enabled { get; set; } = true;

    [Column("sort_order")]
    [DefaultValue(0)]
    public int SortOrder { get; set; }
}
