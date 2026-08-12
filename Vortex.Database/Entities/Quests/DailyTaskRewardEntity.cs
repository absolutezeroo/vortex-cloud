using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Quests;

/// <summary>What one daily task hands over when its reward is claimed.</summary>
[Table("daily_task_rewards")]
[Index(nameof(DailyTaskEntityId))]
public class DailyTaskRewardEntity : VortexEntity
{
    [Column("task_id")]
    public required int DailyTaskEntityId { get; set; }

    /// <summary>Product item type; a short on the wire, so it is one here too.</summary>
    [Column("product_item_type_id")]
    [DefaultValue((short)0)]
    public short ProductItemTypeId { get; set; }

    /// <summary>Reward kind the client localizes.</summary>
    [Column("reward_type_id")]
    public required string RewardTypeId { get; set; }

    [Column("extra_params")]
    [DefaultValue("")]
    public string ExtraParams { get; set; } = string.Empty;

    [Column("amount")]
    [DefaultValue(0)]
    public int Amount { get; set; }

    [ForeignKey(nameof(DailyTaskEntityId))]
    public DailyTaskEntity? DailyTaskEntity { get; set; }
}
