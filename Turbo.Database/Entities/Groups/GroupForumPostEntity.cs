using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Turbo.Database.Entities.Players;
using Turbo.Primitives.Groups.Enums;

namespace Turbo.Database.Entities.Groups;

/// <summary>
/// A single post inside a <see cref="GroupForumThreadEntity"/>. Moderation flips
/// <see cref="State"/>; never hard-deleted (DATA-MODEL §2.6).
/// </summary>
[Table("group_forum_posts")]
[Index(nameof(ThreadEntityId))]
[Index(nameof(GroupEntityId))]
public class GroupForumPostEntity : TurboEntity
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

    [ForeignKey(nameof(ThreadEntityId))]
    public required GroupForumThreadEntity ThreadEntity { get; set; }

    [ForeignKey(nameof(GroupEntityId))]
    public required GroupEntity GroupEntity { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public required PlayerEntity PlayerEntity { get; set; }
}
