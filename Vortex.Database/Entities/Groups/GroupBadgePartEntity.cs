using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Groups;

[Table("group_badge_parts")]
[Index(nameof(PartId), nameof(Type), IsUnique = true)]
public class GroupBadgePartEntity : TurboEntity
{
    /// <summary>Arcturus-compatible part id (stored in badge codes). Unique within its type.</summary>
    [Column("part_id")]
    public required int PartId { get; set; }

    /// <summary>"base" or "symbol".</summary>
    [Column("type")]
    public required string Type { get; set; }

    [Column("file_name")]
    public required string FileName { get; set; }

    [Column("mask_file_name")]
    [DefaultValue("")]
    public required string MaskFileName { get; set; }

    [Column("enabled")]
    [DefaultValue(true)]
    public required bool Enabled { get; set; }
}
