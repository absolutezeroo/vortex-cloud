using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Players;

namespace Vortex.Database.Entities.Groups;

/// <summary>
/// A player barred from (re)joining a guild, created by kicking them with the client's "block" flag
/// set. AS3-verified: the client's <c>MemberData</c> models role type 4 as "blocked", and
/// <c>UnblockGroupMemberMessage</c> is what lifts it.
/// </summary>
[Table("group_blocked_members")]
[Index(nameof(GroupEntityId), nameof(PlayerEntityId), IsUnique = true)]
[Index(nameof(PlayerEntityId))]
public class GroupBlockedMemberEntity : VortexEntity
{
    [Column("group_id")]
    public required int GroupEntityId { get; set; }

    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    /// <summary>Admin who applied the block, kept for the guild audit trail.</summary>
    [Column("blocked_by_player_id")]
    public required int BlockedByPlayerEntityId { get; set; }

    [ForeignKey(nameof(GroupEntityId))]
    public required GroupEntity GroupEntity { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public required PlayerEntity PlayerEntity { get; set; }
}
