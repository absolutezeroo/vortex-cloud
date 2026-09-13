using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Players;

/// <summary>
/// A place an account has already been challenged from and answered correctly, so the challenge is
/// not put again. habbo.com calls these "lieux de connexion autorisés" and gives the settings page a
/// button that clears them all.
/// </summary>
/// <remarks>
/// <para>
/// One row per place, so this one is a list and not a column. What identifies a place is a
/// <see cref="Fingerprint" />: a keyed SHA-256 over the client's address and user agent, hex, 64
/// characters. Keyed, and never the address itself — this table would otherwise be a log of where
/// every player connects from, which is a thing worth not having. The key is the same
/// <c>Vortex:Authentication:IpHashSecret</c> the rest of the auth path hashes addresses with, so
/// rotating it invalidates every trusted location at once, which is the correct behaviour and not a
/// bug.
/// </para>
/// <para>
/// A fingerprint is a weak identifier on purpose: it follows the address, so a player on a mobile
/// network is challenged again when it changes. That is the trade habbo.com makes too — the radio
/// in its unlock dialog offers "just this once" beside "remember this device", because remembering
/// is not always what the visitor wants.
/// </para>
/// </remarks>
[Table("player_account_trusted_locations")]
[Index(nameof(PlayerAccountEntityId), nameof(Fingerprint), IsUnique = true)]
public class PlayerAccountTrustedLocationEntity : VortexEntity
{
    [Column("account_id")]
    public required int PlayerAccountEntityId { get; set; }

    [Column("fingerprint")]
    [StringLength(ContentIdLength)]
    public required string Fingerprint { get; set; }

    /// <summary>
    /// Refreshed every time the place is recognised, so a future eviction pass has something to sort
    /// on. Nothing evicts yet: the list is cleared by the owner, from the settings page.
    /// </summary>
    [Column("last_seen_at")]
    public DateTime LastSeenAt { get; set; }

    [ForeignKey(nameof(PlayerAccountEntityId))]
    public PlayerAccountEntity? PlayerAccount { get; set; }
}
