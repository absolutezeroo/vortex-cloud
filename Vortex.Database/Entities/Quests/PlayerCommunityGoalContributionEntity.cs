using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Players;

namespace Vortex.Database.Entities.Quests;

/// <summary>What one player has contributed to one community goal. The hotel-wide total is the sum
/// of these, and the hall of fame is them ordered by score.</summary>
[Table("player_community_goal_contributions")]
[Index(nameof(PlayerEntityId), nameof(CommunityGoalEntityId), IsUnique = true)]
[Index(nameof(CommunityGoalEntityId), nameof(Score))]
public class PlayerCommunityGoalContributionEntity : VortexEntity
{
    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("goal_id")]
    public required int CommunityGoalEntityId { get; set; }

    [Column("score")]
    [DefaultValue(0)]
    public int Score { get; set; }

    [Column("last_contributed_at")]
    public DateTime? LastContributedAt { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public PlayerEntity? PlayerEntity { get; set; }

    [ForeignKey(nameof(CommunityGoalEntityId))]
    public CommunityGoalEntity? CommunityGoalEntity { get; set; }
}
