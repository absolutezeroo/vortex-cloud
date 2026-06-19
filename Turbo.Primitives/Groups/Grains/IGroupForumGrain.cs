using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Primitives.Groups.Snapshots;
using Turbo.Primitives.Players;

namespace Turbo.Primitives.Groups.Grains;

/// <summary>
/// One grain per group forum (key = group id). Owns thread/post reads, posting and moderation,
/// gated by the group's <c>group_forum_settings</c> permissions. Separate from
/// <see cref="IGroupGrain"/> so the read-heavy forum I/O does not contend with group identity ops.
/// </summary>
public interface IGroupForumGrain : IGrainWithIntegerKey
{
    /// <summary>The forum header + the viewer's permissions; null if the group/forum is missing.</summary>
    Task<ForumSnapshot?> GetForumAsync(PlayerId viewer, CancellationToken ct);

    Task<ForumThreadsPageSnapshot?> GetThreadsAsync(
        PlayerId viewer,
        int startIndex,
        int amount,
        CancellationToken ct
    );

    Task<ThreadMessagesPageSnapshot?> GetMessagesAsync(
        PlayerId viewer,
        int threadId,
        int startIndex,
        int amount,
        CancellationToken ct
    );

    /// <summary>
    /// Posts a message. <paramref name="threadId"/> == 0 starts a new thread (using
    /// <paramref name="title"/> as the subject); otherwise appends to an existing thread. Returns
    /// null if the forum is disabled or the viewer lacks permission.
    /// </summary>
    Task<ForumPostResultSnapshot?> PostAsync(
        PlayerId actor,
        int threadId,
        string title,
        string message,
        CancellationToken ct
    );

    Task<ForumThreadSnapshot?> UpdateThreadAsync(
        PlayerId actor,
        int threadId,
        bool isLocked,
        bool isSticky,
        CancellationToken ct
    );

    Task<ForumThreadSnapshot?> ModerateThreadAsync(
        PlayerId actor,
        int threadId,
        int action,
        CancellationToken ct
    );

    Task<ForumPostSnapshot?> ModerateMessageAsync(
        PlayerId actor,
        int threadId,
        int messageId,
        int action,
        CancellationToken ct
    );

    /// <summary>Updates the forum permission settings (and enables the forum). Owner only.</summary>
    Task<ForumSnapshot?> UpdateSettingsAsync(
        PlayerId actor,
        int readPermission,
        int postMessagePermission,
        int postThreadPermission,
        int moderatePermission,
        CancellationToken ct
    );
}
