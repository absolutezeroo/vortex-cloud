using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Players;

namespace Vortex.Database.Entities.Collectibles;

/// <summary>
/// How many stamps one player holds.
/// </summary>
/// <remarks>
/// Deliberately not a wallet currency. The client never reads stamps from the purse — it asks for
/// them per wallet, through a message of their own — and a fifth currency type would also have to be
/// seeded in <c>currency_types</c> and would then show up wherever the purse is drawn. A balance
/// with exactly one reader belongs in its own row.
/// </remarks>
[Table("player_mint_tokens")]
[Index(nameof(PlayerEntityId), IsUnique = true)]
public class PlayerMintTokensEntity : VortexEntity
{
    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("balance")]
    public int Balance { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public PlayerEntity? PlayerEntity { get; set; }
}
