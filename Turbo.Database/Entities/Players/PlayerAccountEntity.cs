using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Turbo.Database.Entities.Players;

[Table("player_accounts")]
[Index(nameof(Email), IsUnique = true)]
public class PlayerAccountEntity : TurboEntity
{
    [Column("email")]
    public required string Email { get; set; }

    [Column("password_hash")]
    public required string PasswordHash { get; set; }

    /// <summary>Null = not banned. A far-future value (see BanDurations) represents "permanent".</summary>
    [Column("banned_until")]
    public DateTime? BannedUntil { get; set; }

    [Column("ban_reason")]
    [MaxLength(255)]
    public string? BanReason { get; set; }

    /// <summary>Null = not locked. Checked by the (not yet built) trading system.</summary>
    [Column("trading_locked_until")]
    public DateTime? TradingLockedUntil { get; set; }

    [InverseProperty("PlayerAccount")]
    public List<PlayerEntity>? Players { get; set; }
}
