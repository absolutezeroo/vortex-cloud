using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Achievements;

/// <summary>
/// One achievement a resolution statue offers. The catalogue of challenges, not anybody's progress
/// on one — a hotel edits this table to decide what the statue proposes.
/// </summary>
[Table("achievement_resolutions")]
[Index(nameof(AchievementEntityId), IsUnique = true)]
public class AchievementResolutionEntity : VortexEntity
{
    [Column("achievement_id")]
    public required int AchievementEntityId { get; set; }

    /// <summary>
    /// How many levels above the player's current one the challenge asks for. Relative rather than
    /// absolute because the target is computed per player when they pick: an absolute level would
    /// be already-cleared for a veteran and out of reach for a newcomer.
    /// </summary>
    [Column("target_level_offset")]
    public int TargetLevelOffset { get; set; } = 1;

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("enabled")]
    public bool Enabled { get; set; } = true;

    [ForeignKey(nameof(AchievementEntityId))]
    public AchievementEntity? AchievementEntity { get; set; }
}
