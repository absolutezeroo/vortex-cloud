using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Players;
using Vortex.Primitives.Groups.Enums;

namespace Vortex.Database.Entities.Groups;

[Table("group_members")]
[Index(nameof(GroupEntityId), nameof(PlayerEntityId), IsUnique = true)]
[Index(nameof(PlayerEntityId))]
public class GroupMemberEntity : TurboEntity
{
    [Column("group_id")]
    public required int GroupEntityId { get; set; }

    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("rank")]
    [DefaultValue(GroupMemberRank.Member)]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required GroupMemberRank Rank { get; set; }

    [ForeignKey(nameof(GroupEntityId))]
    public required GroupEntity GroupEntity { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public required PlayerEntity PlayerEntity { get; set; }
}
