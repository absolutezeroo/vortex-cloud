using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Players;

namespace Vortex.Database.Entities.Messenger;

[Table("messenger_ignored")]
[Index(nameof(PlayerEntityId), nameof(IgnoredPlayerEntityId), IsUnique = true)]
public class MessengerIgnoredEntity : VortexEntity
{
    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("ignored_player_id")]
    public required int IgnoredPlayerEntityId { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public required PlayerEntity PlayerEntity { get; set; }

    [ForeignKey(nameof(IgnoredPlayerEntityId))]
    public required PlayerEntity IgnoredPlayerEntity { get; set; }
}
