using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Primitives.Players.Enums.Wallet;

namespace Vortex.Database.Entities.Catalog;

[Table("catalog_vouchers")]
[Index(nameof(Code), IsUnique = true)]
public class VoucherEntity : VortexEntity
{
    [Column("code")]
    [MaxLength(64)]
    public required string Code { get; set; }

    [Column("currency_type")]
    public required CurrencyType CurrencyType { get; set; }

    [Column("activity_point_type")]
    public int? ActivityPointType { get; set; }

    [Column("amount")]
    public required int Amount { get; set; }

    [Column("max_redemptions")]
    public int? MaxRedemptions { get; set; }

    [Column("is_active")]
    [DefaultValue(true)]
    public required bool IsActive { get; set; }

    [Column("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    [Column("created_by")]
    [MaxLength(255)]
    public required string CreatedBy { get; set; }
}
