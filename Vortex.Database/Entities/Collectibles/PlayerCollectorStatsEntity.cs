using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Players;

namespace Vortex.Database.Entities.Collectibles;

/// <summary>
/// A player's best collector score. Kept because the live score falls when furniture is sold or
/// traded away, and the client shows a highest score beside it that is not supposed to.
/// </summary>
[Table("player_collector_stats")]
[Index(nameof(PlayerEntityId), IsUnique = true)]
public class PlayerCollectorStatsEntity : VortexEntity
{
    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("highest_score")]
    public int HighestScore { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public PlayerEntity? PlayerEntity { get; set; }
}
