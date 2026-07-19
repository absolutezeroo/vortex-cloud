using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Achievements;

/// <summary>
/// Definition header for a multi-level achievement. <see cref="Name"/> is the identifier the client
/// uses to build a level's badge code as <c>"ACH_" + Name + level</c> (see the client's
/// AchievementData badge-code parsing), and <see cref="Category"/> is the display tab it is grouped
/// under in the achievements window. Per-level data lives in <see cref="AchievementLevelEntity"/>.
/// </summary>
[Table("achievements")]
[Index(nameof(Name), IsUnique = true)]
public class AchievementEntity : TurboEntity
{
    [Column("name")]
    public required string Name { get; set; }

    [Column("category")]
    public required string Category { get; set; }

    /// <summary>Client display hint sent as the achievement's <c>displayMethod</c>. 0 = default.</summary>
    [Column("display_method")]
    public int DisplayMethod { get; set; }

    [InverseProperty(nameof(AchievementLevelEntity.AchievementEntity))]
    public List<AchievementLevelEntity>? Levels { get; set; }
}
