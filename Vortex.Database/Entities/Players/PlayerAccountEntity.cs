using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Players;

[Table("player_accounts")]
[Index(nameof(Email), IsUnique = true)]
public class PlayerAccountEntity : VortexEntity
{
    [Column("email")]
    public required string Email { get; set; }

    [Column("password_hash")]
    public required string PasswordHash { get; set; }

    /// <summary>
    /// Base32 TOTP secret for the dashboard's second factor, or null when the account has none. Only
    /// written once an authenticator has proved it holds the same secret, so a row with a value here
    /// is a factor that actually works. Stored as it is: the password hash sits in the same table, so
    /// anything that can read this column can already read that.
    /// </summary>
    [Column("totp_secret")]
    [StringLength(64)]
    public string? TotpSecret { get; set; }

    [InverseProperty("PlayerAccount")]
    public List<PlayerEntity>? Players { get; set; }
}
