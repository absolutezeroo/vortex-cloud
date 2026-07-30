using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Pets;

/// <summary>
/// One line a pet can say. Habbo keys these <c>pet.vocals.&lt;type&gt;.&lt;vocal&gt;.&lt;index&gt;</c>
/// in the client's text bundle, but nothing in the client resolves them -- the server picks a line
/// and sends it as ordinary room chat from the pet. They live here rather than in code because they
/// are content: an operator translates them or writes new ones without a rebuild.
/// </summary>
[Table("pet_vocals")]
[Index(nameof(PetType), nameof(VocalType))]
public class PetVocalEntity : VortexEntity
{
    [Column("pet_type")]
    public required int PetType { get; set; }

    /// <summary>The situation that triggers the line, for example <c>GENERIC_HAPPY</c> or
    /// <c>LEVEL_UP</c>. Matches the key Habbo uses.</summary>
    [Column("vocal_type")]
    public required string VocalType { get; set; }

    [Column("text")]
    public required string Text { get; set; }
}
