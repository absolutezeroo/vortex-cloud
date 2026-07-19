using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Players;

namespace Vortex.Database.Entities.Quests;

/// <summary>
/// A player's state on a single quest: how many objective steps are done, whether it has been
/// accepted (started), and whether it is completed.
/// </summary>
[Table("player_quests")]
[Index(nameof(PlayerEntityId), nameof(QuestEntityId), IsUnique = true)]
public class PlayerQuestEntity : TurboEntity
{
    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("quest_id")]
    public required int QuestEntityId { get; set; }

    [Column("completed_steps")]
    [DefaultValue(0)]
    public int CompletedSteps { get; set; }

    [Column("accepted")]
    [DefaultValue(false)]
    public bool Accepted { get; set; }

    [Column("completed")]
    [DefaultValue(false)]
    public bool Completed { get; set; }

    [Column("accepted_at")]
    public DateTime? AcceptedAt { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public PlayerEntity? PlayerEntity { get; set; }

    [ForeignKey(nameof(QuestEntityId))]
    public QuestEntity? QuestEntity { get; set; }
}
