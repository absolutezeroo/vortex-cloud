using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Players;
using Vortex.Database.Entities.Room;
using Vortex.Primitives.Groups.Enums;

namespace Vortex.Database.Entities.Groups;

[Table("groups")]
[Index(nameof(RoomEntityId), IsUnique = true)]
[Index(nameof(OwnerPlayerEntityId))]
public class GroupEntity : TurboEntity
{
    [Column("name")]
    [MaxLength(50)]
    public required string Name { get; set; }

    [Column("description")]
    [MaxLength(255)]
    public string? Description { get; set; }

    [Column("badge")]
    [MaxLength(100)]
    public required string Badge { get; set; }

    [Column("room_id")]
    public required int RoomEntityId { get; set; }

    [Column("player_id")]
    public required int OwnerPlayerEntityId { get; set; }

    [Column("type")]
    [DefaultValue(GroupType.Open)]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required GroupType Type { get; set; }

    [Column("color_one")]
    [MaxLength(12)]
    public required string ColorOne { get; set; }

    [Column("color_two")]
    [MaxLength(12)]
    public required string ColorTwo { get; set; }

    [Column("admin_only_decoration")]
    [DefaultValue(false)]
    public required bool AdminOnlyDecoration { get; set; }

    public required RoomEntity RoomEntity { get; set; }

    [ForeignKey(nameof(OwnerPlayerEntityId))]
    public required PlayerEntity OwnerPlayerEntity { get; set; }

    [InverseProperty(nameof(GroupMemberEntity.GroupEntity))]
    public List<GroupMemberEntity>? Members { get; set; }

    [InverseProperty(nameof(GroupForumSettingsEntity.GroupEntity))]
    public GroupForumSettingsEntity? ForumSettings { get; set; }
}
