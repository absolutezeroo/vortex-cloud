using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Players;

[Table("player_vault_income_rewards")]
[Index(nameof(PlayerEntityId))]
public class PlayerVaultIncomeRewardEntity : TurboEntity
{
    [Column("player_id")]
    public int PlayerEntityId { get; set; }

    [Column("reward_category")]
    public int RewardCategory { get; set; }

    [Column("reward_type")]
    public int RewardType { get; set; }

    [Column("amount")]
    public int Amount { get; set; }

    [Column("product_code")]
    public string ProductCode { get; set; } = string.Empty;

    [ForeignKey(nameof(PlayerEntityId))]
    public PlayerEntity? PlayerEntity { get; set; }
}
