using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Players;

namespace Vortex.Database.Entities.Messenger;

[Table("messenger_requests")]
[Index(nameof(PlayerEntityId), nameof(RequestedPlayerEntityId), IsUnique = true)]
public class MessengerRequestEntity : VortexEntity
{
    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("requested_id")]
    public required int RequestedPlayerEntityId { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public required PlayerEntity PlayerEntity { get; set; }

    [ForeignKey(nameof(RequestedPlayerEntityId))]
    public required PlayerEntity RequestedPlayerEntity { get; set; }
}
