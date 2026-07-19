using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Players;

namespace Vortex.Database.Entities.Catalog;

[Table("catalog_voucher_redemptions")]
[Index(nameof(VoucherEntityId), nameof(PlayerEntityId), IsUnique = true)]
public class VoucherRedemptionEntity : VortexEntity
{
    [Column("voucher_id")]
    public required int VoucherEntityId { get; set; }

    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("redeemed_at")]
    public required DateTime RedeemedAt { get; set; }

    [ForeignKey(nameof(VoucherEntityId))]
    public VoucherEntity? VoucherEntity { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public PlayerEntity? PlayerEntity { get; set; }
}
