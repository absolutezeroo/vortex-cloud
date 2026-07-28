using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Entities.Players;

namespace Vortex.Database.Entities.Groups;

/// <summary>
/// How far a player has read into a guild's forum. AS3-verified: the client's
/// <c>ForumData.lastReadMessageId</c> is defined as <c>totalMessages - unreadMessages</c>, so this
/// is a running COUNT of messages read, not a <c>group_forum_posts.id</c>. Unread is therefore
/// <c>max(0, totalMessages - ReadMessageCount)</c>.
/// </summary>
[Table("group_forum_read_markers")]
[Index(nameof(GroupEntityId), nameof(PlayerEntityId), IsUnique = true)]
[Index(nameof(PlayerEntityId))]
public class GroupForumReadMarkerEntity : VortexEntity
{
    [Column("group_id")]
    public required int GroupEntityId { get; set; }

    [Column("player_id")]
    public required int PlayerEntityId { get; set; }

    [Column("read_message_count")]
    [DefaultValue(0)]
    public required int ReadMessageCount { get; set; }

    [ForeignKey(nameof(GroupEntityId))]
    public required GroupEntity GroupEntity { get; set; }

    [ForeignKey(nameof(PlayerEntityId))]
    public required PlayerEntity PlayerEntity { get; set; }
}
