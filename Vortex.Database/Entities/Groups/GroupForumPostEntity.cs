using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Players;
using Vortex.Primitives.Groups.Enums;

namespace Vortex.Database.Entities.Groups;

/// <summary>
/// A single post inside a <see cref="GroupForumThreadEntity"/>. Moderation flips
/// <see cref="State"/>; never hard-deleted (DATA-MODEL §2.6).
/// </summary>
[Table("group_forum_posts")]
[Index(nameof(ThreadEntityId))]
[Index(nameof(GroupEntityId))]
public class GroupForumPostEntity : VortexEntity
{
    [Column("thread_id")]
    public required int ThreadEntityId { get; set; }

    [Column("group_id")]
    public required int GroupEntityId { get; set; }

    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("message", TypeName = "text")]
    public required string Message { get; set; }

    [Column("state")]
    [DefaultValue(GroupForumPostState.Visible)]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required GroupForumPostState State { get; set; }

    /// <summary>Admin who last moderated this post; surfaced to the client so the forum can show
    /// "hidden by X". Null until the post is moderated.</summary>
    [Column("admin_player_id")]
    public int? AdminPlayerEntityId { get; set; }

    [Column("admin_operation_at")]
    public DateTime? AdminOperationAt { get; set; }

    [ForeignKey(nameof(ThreadEntityId))]
    public required GroupForumThreadEntity ThreadEntity { get; set; }

    [ForeignKey(nameof(AdminPlayerEntityId))]
    public PlayerEntity? AdminPlayerEntity { get; set; }

    [ForeignKey(nameof(GroupEntityId))]
    public required GroupEntity GroupEntity { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public required PlayerEntity PlayerEntity { get; set; }
}
