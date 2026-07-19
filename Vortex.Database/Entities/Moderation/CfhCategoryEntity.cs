using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vortex.Database.Entities.Moderation;

/// <summary>Top-level grouping shown in the staff CFH tool's report-topic picker
/// (CfhTopicsInitMessageEvent's "categories" list).</summary>
[Table("cfh_categories")]
public class CfhCategoryEntity : TurboEntity
{
    [Column("name")]
    [MaxLength(100)]
    public required string Name { get; set; }

    [Column("display_order")]
    public int DisplayOrder { get; set; }
}
