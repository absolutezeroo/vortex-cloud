using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Fishing;

/// <summary>
/// One player's standing in one derby.
/// </summary>
/// <remarks>
/// The row exists from the moment the player joins, with a score of zero, because joining is what
/// the client is told about and a leaderboard that hides its entrants until they score reads as
/// broken. Vortex's own addition — see <see cref="FishingDerbyEntity"/>.
/// </remarks>
[Table("fishing_derby_entries")]
[Index(nameof(DerbyId), nameof(PlayerId), IsUnique = true)]
[Index(nameof(DerbyId), nameof(BestWeight))]
public class FishingDerbyEntryEntity : VortexEntity
{
    [Column("derby_id")]
    public required int DerbyId { get; set; }

    [Column("player_id")]
    public required int PlayerId { get; set; }

    /// <summary>Heaviest single catch during the derby. A total would reward whoever idled longest.</summary>
    [Column("best_weight")]
    public int BestWeight { get; set; }
}
