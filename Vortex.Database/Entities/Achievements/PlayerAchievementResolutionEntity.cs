using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Players;

namespace Vortex.Database.Entities.Achievements;

/// <summary>
/// A challenge a player took on at one statue.
///
/// Keyed on the item, not on the player: the client addresses every one of these by the statue's
/// object id, two statues are two independent challenges, and the reset button resets one of them.
/// A finished or expired row is kept rather than deleted — the statue keeps showing its result, and
/// the same achievement must not be challengeable twice on the same item.
/// </summary>
[Table("player_achievement_resolutions")]
[Index(nameof(ItemEntityId), IsUnique = true)]
[Index(nameof(PlayerEntityId))]
public class PlayerAchievementResolutionEntity : VortexEntity
{
    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    /// <summary>The statue's item id — what the client calls the stuff id.</summary>
    [Column("item_id")]
    public required int ItemEntityId { get; set; }

    [Column("achievement_id")]
    public required int AchievementEntityId { get; set; }

    /// <summary>Levels completed that the player must reach. Frozen when they pick, so later edits
    /// to the offer's offset never move a challenge already under way.</summary>
    [Column("target_level")]
    public required int TargetLevel { get; set; }

    [Column("started_at")]
    public required DateTime StartedAt { get; set; }

    [Column("ends_at")]
    public required DateTime EndsAt { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    /// <summary>Badge awarded on completion, stamped at the time so the record survives an edit to
    /// the achievement's levels.</summary>
    [Column("awarded_badge_code")]
    public string? AwardedBadgeCode { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public PlayerEntity? PlayerEntity { get; set; }

    [ForeignKey(nameof(AchievementEntityId))]
    public AchievementEntity? AchievementEntity { get; set; }
}
