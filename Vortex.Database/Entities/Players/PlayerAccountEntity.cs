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

    /// <summary>
    /// The account's safety lock: while it is set, the account cannot spend — no catalog purchase,
    /// no marketplace.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It is what a player reaches for when they believe someone else is in their account: the
    /// thief holds the session, but the lock needs the password (and the second factor, when there
    /// is one) to come off, so the credits stay where they are until the owner sorts it out.
    /// </para>
    /// <para>
    /// On the ACCOUNT and not the avatar, because that is the thing being protected — every avatar
    /// it owns spends the same purse. The client learns it twice: in the user object at login, and
    /// through <c>AccountSafetyLockStatusChangeMessageComposer</c> when it changes mid-session,
    /// which is the case that matters — the thief is connected NOW.
    /// </para>
    /// </remarks>
    [Column("safety_locked")]
    public bool SafetyLocked { get; set; }

    [InverseProperty("PlayerAccount")]
    public List<PlayerEntity>? Players { get; set; }
}
