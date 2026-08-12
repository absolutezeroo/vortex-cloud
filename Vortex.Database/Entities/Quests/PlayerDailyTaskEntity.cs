using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Players;
using Vortex.Primitives.Quests;

namespace Vortex.Database.Entities.Quests;

/// <summary>
/// One daily task assigned to one player for one day. The row's own id is what goes on the wire as
/// the task id: the client patches assignments by that id, and two players working the same
/// definition must not share one.
/// </summary>
[Table("player_daily_tasks")]
[Index(nameof(PlayerEntityId), nameof(AssignedOn))]
[Index(nameof(PlayerEntityId), nameof(DailyTaskEntityId), nameof(AssignedOn), IsUnique = true)]
public class PlayerDailyTaskEntity : VortexEntity
{
    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("task_id")]
    public required int DailyTaskEntityId { get; set; }

    /// <summary>The day this assignment belongs to, so a new board is drawn once per calendar day.</summary>
    [Column("assigned_on")]
    public required DateOnly AssignedOn { get; set; }

    [Column("repeats")]
    [DefaultValue(0)]
    public int Repeats { get; set; }

    [Column("status")]
    public required DailyTaskStatus Status { get; set; }

    /// <summary>When the assignment lapses; the wire's seconds-left is this minus now.</summary>
    [Column("expires_at")]
    public required DateTime ExpiresAt { get; set; }

    [Column("claimed_at")]
    public DateTime? ClaimedAt { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public PlayerEntity? PlayerEntity { get; set; }

    [ForeignKey(nameof(DailyTaskEntityId))]
    public DailyTaskEntity? DailyTaskEntity { get; set; }
}
