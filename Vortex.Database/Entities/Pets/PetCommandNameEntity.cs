using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Pets;

/// <summary>
/// The display name of a pet command, as the client shows it on the training buttons.
///
/// The name is per command id and shared by every pet type, unlike <see cref="PetCommandEntity"/>
/// which configures a command for one type. It exists because AS3 has no "issue pet command"
/// packet: PetCommandTool posts the command as ordinary chat, "&lt;pet name&gt; &lt;command&gt;",
/// so the server has to recognise the words a player typed.
/// </summary>
[Table("pet_command_names")]
[Index(nameof(CommandId), IsUnique = true)]
public class PetCommandNameEntity : VortexEntity
{
    [Column("command")]
    public required int CommandId { get; set; }

    [Column("name")]
    public required string Name { get; set; }
}
