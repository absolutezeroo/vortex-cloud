using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Players;

/// <summary>
/// One word on a player's personal chat filter. Unlike
/// <see cref="PlayerAccountPreferencesEntity"/>, which is one row of scalars per player, this is a
/// collection: the client's word-filter dialog adds and removes entries one at a time and keys
/// every operation on the word itself, never on a row id.
/// </summary>
/// <remarks>
/// The unique index is on the pair, not on the word alone — two players may filter the same word —
/// and it is what makes an add idempotent, which the client relies on: it refuses to send a word
/// already on its own copy of the list, but two clients on one account can still race.
/// </remarks>
[Table("player_word_filters")]
[Index(nameof(PlayerEntityId), nameof(Word), IsUnique = true)]
public class PlayerWordFilterEntity : VortexEntity
{
    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    // Capped to match PlayerGrain.MaxWordFilterWordLength, which trims before writing: the default
    // varchar(512) would put a 2 KB pair in the unique index for no reason.
    [Column("word")]
    [MaxLength(64)]
    public required string Word { get; set; }
}
