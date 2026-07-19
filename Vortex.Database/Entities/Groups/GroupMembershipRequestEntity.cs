using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Players;

namespace Vortex.Database.Entities.Groups;

/// <summary>
/// Pending join request for an <see cref="Vortex.Primitives.Groups.Enums.GroupType.Exclusive"/>
/// group (owner/admins approve or decline). See DATA-MODEL §2.3.
/// </summary>
[Table("group_membership_requests")]
[Index(nameof(GroupEntityId), nameof(PlayerEntityId), IsUnique = true)]
[Index(nameof(PlayerEntityId))]
public class GroupMembershipRequestEntity : TurboEntity
{
    [Column("group_id")]
    public required int GroupEntityId { get; set; }

    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [ForeignKey(nameof(GroupEntityId))]
    public required GroupEntity GroupEntity { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public required PlayerEntity PlayerEntity { get; set; }
}
