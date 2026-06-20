using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Turbo.Database.Entities.Players;
using Turbo.Primitives.Groups.Enums;

namespace Turbo.Database.Entities.Groups;

/// <summary>
/// A forum thread inside a group forum. Moderation flips <see cref="State"/>; never hard-deleted
/// (DATA-MODEL §2.5).
/// </summary>
[Table("group_forum_threads")]
[Index(nameof(GroupEntityId))]
[Index(nameof(LastPostAt))]
public class GroupForumThreadEntity : TurboEntity
{
    [Column("group_id")]
    public required int GroupEntityId { get; set; }

    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("subject")]
    [MaxLength(255)]
    public required string Subject { get; set; }

    [Column("state")]
    [DefaultValue(GroupForumThreadState.Open)]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required GroupForumThreadState State { get; set; }

    [Column("is_pinned")]
    [DefaultValue(false)]
    public required bool IsPinned { get; set; }

    [Column("post_count")]
    [DefaultValue(0)]
    public required int PostCount { get; set; }

    [Column("last_post_at")]
    public DateTime? LastPostAt { get; set; }

    [Column("last_post_player_id")]
    public int? LastPostPlayerEntityId { get; set; }

    [ForeignKey(nameof(GroupEntityId))]
    public required GroupEntity GroupEntity { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public required PlayerEntity PlayerEntity { get; set; }

    [ForeignKey(nameof(LastPostPlayerEntityId))]
    public PlayerEntity? LastPostPlayerEntity { get; set; }

    [InverseProperty(nameof(GroupForumPostEntity.ThreadEntity))]
    public List<GroupForumPostEntity>? Posts { get; set; }
}
