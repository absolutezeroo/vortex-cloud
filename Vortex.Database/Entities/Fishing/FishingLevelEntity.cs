using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Fishing;

/// <summary>
/// One fishing level — the progression that unlocks zones, and nothing else observed.
/// </summary>
/// <remarks>
/// Separate from <see cref="FishingRodTierEntity"/> on purpose; see that class for why. The curve's
/// real numbers are unknown, so it is a table rather than a formula: an operator retunes it without
/// a deploy.
/// </remarks>
[Table("fishing_levels")]
[Index(nameof(Level), IsUnique = true)]
public class FishingLevelEntity : VortexEntity
{
    [Column("level")]
    public required int Level { get; set; }

    /// <summary>Cumulative <em>fishing</em> XP at which this level begins.</summary>
    [Column("xp_threshold")]
    public int XpThreshold { get; set; }
}
