using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Catalog;

namespace Vortex.Database.Entities.Players;

[Table("player_currencies")]
[Index(nameof(PlayerEntityId), nameof(CurrencyTypeEntityId), IsUnique = true)]
public class PlayerCurrencyEntity : VortexEntity
{
    [Column("player_id")]
    public int PlayerEntityId { get; set; }

    [Column("currency_type_id")]
    public int CurrencyTypeEntityId { get; set; }

    [Column("amount")]
    [DefaultValue(0)]
    public required int Amount { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public PlayerEntity? PlayerEntity { get; set; }

    [ForeignKey(nameof(CurrencyTypeEntityId))]
    public CurrencyTypeEntity? CurrencyTypeEntity { get; set; }
}
