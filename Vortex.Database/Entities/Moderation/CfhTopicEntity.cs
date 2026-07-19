using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Vortex.Database.Entities.Permissions;

namespace Vortex.Database.Entities.Moderation;

/// <summary>A reportable reason within a <see cref="CfhCategoryEntity"/>. The client picks one of
/// these when submitting a report (its wire "topicId") — <see cref="DefaultSanctionPresetEntityId"/>
/// is what CloseIssueDefaultActionMessageHandler/DefaultSanctionMessageHandler apply, mirroring
/// Daybreak's topic-linked default sanction (reviewed for behavior only, not code).</summary>
[Table("cfh_topics")]
public class CfhTopicEntity : VortexEntity
{
    [Column("category_id")]
    public required int CfhCategoryEntityId { get; set; }

    [Column("name")]
    [MaxLength(100)]
    public required string Name { get; set; }

    /// <summary>Short human-readable consequence text shown to staff (e.g. "Ban 1 day").</summary>
    [Column("consequence")]
    [MaxLength(255)]
    public string? Consequence { get; set; }

    [Column("default_sanction_preset_id")]
    public int? DefaultSanctionPresetEntityId { get; set; }

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    [ForeignKey(nameof(CfhCategoryEntityId))]
    public CfhCategoryEntity? CfhCategoryEntity { get; set; }

    [ForeignKey(nameof(DefaultSanctionPresetEntityId))]
    public SanctionPresetEntity? DefaultSanctionPreset { get; set; }
}
