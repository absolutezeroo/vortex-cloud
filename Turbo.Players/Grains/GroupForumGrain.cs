using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Turbo.Database.Context;
using Turbo.Database.Entities.Groups;
using Turbo.Database.Entities.Players;
using Turbo.Primitives.Events;
using Turbo.Primitives.Groups.Enums;
using Turbo.Primitives.Groups.Grains;
using Turbo.Primitives.Groups.Snapshots;
using Turbo.Primitives.Players;

namespace Turbo.Players.Grains;

internal sealed class GroupForumGrain(
    IDbContextFactory<TurboDbContext> dbCtxFactory,
    IEventPublisher events,
    ILogger<GroupForumGrain> logger
) : Grain, IGroupForumGrain
{
    private readonly ILogger<GroupForumGrain> _logger = logger;

    private const string DeniedError = "You are not allowed to do that.";
    private const string DisabledError = "This forum is disabled.";

    private enum ForumRole
    {
        None = 0,
        Member = 1,
        Admin = 2,
        Owner = 3,
    }

    private int GroupId => (int)this.GetPrimaryKeyLong();

    public async Task<ForumSnapshot?> GetForumAsync(PlayerId viewer, CancellationToken ct)
    {
        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        GroupEntity? group = await dbCtx
            .Groups.AsNoTracking()
            .Include(g => g.ForumSettings)
            .FirstOrDefaultAsync(g => g.Id == GroupId && g.DeletedAt == null, ct);
        if (group is null || group.ForumSettings is null)
        {
            return null;
        }

        ForumRole role = await GetRoleAsync(dbCtx, group, viewer.Value, ct);
        return await BuildForumSnapshotAsync(dbCtx, group, group.ForumSettings, role, ct);
    }

    public async Task<ForumThreadsPageSnapshot?> GetThreadsAsync(
        PlayerId viewer,
        int startIndex,
        int amount,
        CancellationToken ct
    )
    {
        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        GroupEntity? group = await dbCtx
            .Groups.AsNoTracking()
            .Include(g => g.ForumSettings)
            .FirstOrDefaultAsync(g => g.Id == GroupId && g.DeletedAt == null, ct);
        if (group is null || group.ForumSettings is null)
        {
            return null;
        }

        ForumRole role = await GetRoleAsync(dbCtx, group, viewer.Value, ct);
        if (!CanRead(group.ForumSettings, role))
        {
            return null;
        }

        int take = NormalizeAmount(amount);
        int skip = Math.Max(startIndex, 0);

        List<GroupForumThreadEntity> threads = await dbCtx
            .GroupForumThreads.AsNoTracking()
            .Include(t => t.PlayerEntity)
            .Include(t => t.LastPostPlayerEntity)
            .Where(t =>
                t.GroupEntityId == GroupId
                && t.DeletedAt == null
                && t.State != GroupForumThreadState.Hidden
            )
            // Pinned first, then most recent activity.
            .OrderByDescending(t => t.IsPinned)
            .ThenByDescending(t => t.LastPostAt ?? t.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        DateTime now = DateTime.UtcNow;
        return new ForumThreadsPageSnapshot
        {
            GroupId = GroupId,
            StartIndex = startIndex,
            Threads = threads.Select(t => BuildThreadSnapshot(t, now)).ToList(),
        };
    }

    public async Task<ThreadMessagesPageSnapshot?> GetMessagesAsync(
        PlayerId viewer,
        int threadId,
        int startIndex,
        int amount,
        CancellationToken ct
    )
    {
        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        GroupEntity? group = await dbCtx
            .Groups.AsNoTracking()
            .Include(g => g.ForumSettings)
            .FirstOrDefaultAsync(g => g.Id == GroupId && g.DeletedAt == null, ct);
        if (group is null || group.ForumSettings is null)
        {
            return null;
        }

        ForumRole role = await GetRoleAsync(dbCtx, group, viewer.Value, ct);
        if (!CanRead(group.ForumSettings, role))
        {
            return null;
        }

        int take = NormalizeAmount(amount);
        int skip = Math.Max(startIndex, 0);

        List<GroupForumPostEntity> posts = await dbCtx
            .GroupForumPosts.AsNoTracking()
            .Include(p => p.PlayerEntity)
            .Where(p =>
                p.ThreadEntityId == threadId && p.GroupEntityId == GroupId && p.DeletedAt == null
            )
            .OrderBy(p => p.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        Dictionary<int, int> authorPostCounts = await GetAuthorPostCountsAsync(dbCtx, posts, ct);

        DateTime now = DateTime.UtcNow;
        List<ForumPostSnapshot> messages = posts
            .Select(
                (p, i) =>
                    BuildPostSnapshot(
                        p,
                        skip + i,
                        authorPostCounts.GetValueOrDefault(p.PlayerEntityId, 0),
                        now
                    )
            )
            .ToList();

        return new ThreadMessagesPageSnapshot
        {
            GroupId = GroupId,
            ThreadId = threadId,
            StartIndex = startIndex,
            Messages = messages,
        };
    }

    public async Task<ForumPostResultSnapshot?> PostAsync(
        PlayerId actor,
        int threadId,
        string title,
        string message,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        GroupEntity? group = await dbCtx
            .Groups.Include(g => g.ForumSettings)
            .FirstOrDefaultAsync(g => g.Id == GroupId && g.DeletedAt == null, ct);
        if (group is null || group.ForumSettings is null || !group.ForumSettings.Enabled)
        {
            return null;
        }

        int actorId = actor.Value;
        ForumRole role = await GetRoleAsync(dbCtx, group, actorId, ct);

        PlayerEntity? actorEntity = await dbCtx.Players.FindAsync([actorId], ct);
        if (actorEntity is null)
        {
            return null;
        }

        DateTime now = DateTime.UtcNow;

        if (threadId == 0)
        {
            if (!Allows(group.ForumSettings.ThreadPermission, role))
            {
                return null;
            }

            GroupForumThreadEntity thread = new GroupForumThreadEntity
            {
                GroupEntityId = GroupId,
                PlayerEntityId = actorId,
                Subject = string.IsNullOrWhiteSpace(title) ? "(no subject)" : title,
                State = GroupForumThreadState.Open,
                IsPinned = false,
                PostCount = 1,
                LastPostAt = now,
                LastPostPlayerEntityId = actorId,
                GroupEntity = group,
                PlayerEntity = actorEntity,
            };
            dbCtx.GroupForumThreads.Add(thread);
            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

            dbCtx.GroupForumPosts.Add(
                new GroupForumPostEntity
                {
                    ThreadEntityId = thread.Id,
                    GroupEntityId = GroupId,
                    PlayerEntityId = actorId,
                    Message = message,
                    State = GroupForumPostState.Visible,
                    ThreadEntity = thread,
                    GroupEntity = group,
                    PlayerEntity = actorEntity,
                }
            );
            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

            await events
                .PublishAsync(new ForumThreadCreatedEvent(actorId, GroupId, thread.Id), ct)
                .ConfigureAwait(false);

            return new ForumPostResultSnapshot
            {
                GroupId = GroupId,
                IsNewThread = true,
                ThreadId = thread.Id,
                Thread = BuildThreadSnapshot(
                    thread,
                    now,
                    authorName: actorEntity.Name,
                    lastPostName: actorEntity.Name
                ),
            };
        }
        else
        {
            if (!Allows(group.ForumSettings.PostPermission, role))
            {
                return null;
            }

            GroupForumThreadEntity? thread = await dbCtx.GroupForumThreads.FirstOrDefaultAsync(
                t => t.Id == threadId && t.GroupEntityId == GroupId && t.DeletedAt == null,
                ct
            );
            if (thread is null || thread.State == GroupForumThreadState.Hidden)
            {
                return null;
            }

            // A locked thread only accepts posts from moderators.
            if (
                thread.State == GroupForumThreadState.Locked
                && !Allows(group.ForumSettings.ModPermission, role)
            )
            {
                return null;
            }

            GroupForumPostEntity post = new GroupForumPostEntity
            {
                ThreadEntityId = thread.Id,
                GroupEntityId = GroupId,
                PlayerEntityId = actorId,
                Message = message,
                State = GroupForumPostState.Visible,
                ThreadEntity = thread,
                GroupEntity = group,
                PlayerEntity = actorEntity,
            };
            dbCtx.GroupForumPosts.Add(post);

            thread.PostCount += 1;
            thread.LastPostAt = now;
            thread.LastPostPlayerEntityId = actorId;
            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

            await events
                .PublishAsync(new ForumPostCreatedEvent(actorId, GroupId, thread.Id, post.Id), ct)
                .ConfigureAwait(false);

            int authorPostCount = await dbCtx.GroupForumPosts.CountAsync(
                p =>
                    p.GroupEntityId == GroupId
                    && p.PlayerEntityId == actorId
                    && p.State == GroupForumPostState.Visible
                    && p.DeletedAt == null,
                ct
            );

            return new ForumPostResultSnapshot
            {
                GroupId = GroupId,
                IsNewThread = false,
                ThreadId = thread.Id,
                Post = BuildPostSnapshot(
                    post,
                    thread.PostCount - 1,
                    authorPostCount,
                    now,
                    actorEntity
                ),
            };
        }
    }

    public async Task<ForumThreadSnapshot?> UpdateThreadAsync(
        PlayerId actor,
        int threadId,
        bool isLocked,
        bool isSticky,
        CancellationToken ct
    )
    {
        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        (GroupEntity? group, ForumRole role) = await LoadForModerationAsync(dbCtx, actor, ct);
        if (group is null)
        {
            return null;
        }

        GroupForumThreadEntity? thread = await dbCtx
            .GroupForumThreads.Include(t => t.PlayerEntity)
            .Include(t => t.LastPostPlayerEntity)
            .FirstOrDefaultAsync(
                t => t.Id == threadId && t.GroupEntityId == GroupId && t.DeletedAt == null,
                ct
            );
        if (thread is null)
        {
            return null;
        }

        thread.IsPinned = isSticky;
        // Preserve a Hidden state; otherwise toggle Locked/Open.
        if (thread.State != GroupForumThreadState.Hidden)
        {
            thread.State = isLocked ? GroupForumThreadState.Locked : GroupForumThreadState.Open;
        }

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        await events
            .PublishAsync(
                new ForumThreadModeratedEvent(actor.Value, GroupId, thread.Id, (int)thread.State),
                ct
            )
            .ConfigureAwait(false);

        return BuildThreadSnapshot(thread, DateTime.UtcNow);
    }

    public async Task<ForumThreadSnapshot?> ModerateThreadAsync(
        PlayerId actor,
        int threadId,
        int action,
        CancellationToken ct
    )
    {
        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        (GroupEntity? group, ForumRole role) = await LoadForModerationAsync(dbCtx, actor, ct);
        if (group is null)
        {
            return null;
        }

        GroupForumThreadEntity? thread = await dbCtx
            .GroupForumThreads.Include(t => t.PlayerEntity)
            .Include(t => t.LastPostPlayerEntity)
            .FirstOrDefaultAsync(
                t => t.Id == threadId && t.GroupEntityId == GroupId && t.DeletedAt == null,
                ct
            );
        if (thread is null)
        {
            return null;
        }

        thread.State = MapThreadAction(action);
        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        await events
            .PublishAsync(
                new ForumThreadModeratedEvent(actor.Value, GroupId, thread.Id, (int)thread.State),
                ct
            )
            .ConfigureAwait(false);

        return BuildThreadSnapshot(thread, DateTime.UtcNow);
    }

    public async Task<ForumPostSnapshot?> ModerateMessageAsync(
        PlayerId actor,
        int threadId,
        int messageId,
        int action,
        CancellationToken ct
    )
    {
        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        (GroupEntity? group, ForumRole role) = await LoadForModerationAsync(dbCtx, actor, ct);
        if (group is null)
        {
            return null;
        }

        GroupForumPostEntity? post = await dbCtx
            .GroupForumPosts.Include(p => p.PlayerEntity)
            .FirstOrDefaultAsync(
                p =>
                    p.Id == messageId
                    && p.ThreadEntityId == threadId
                    && p.GroupEntityId == GroupId
                    && p.DeletedAt == null,
                ct
            );
        if (post is null)
        {
            return null;
        }

        post.State = MapPostAction(action);
        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        await events
            .PublishAsync(
                new ForumPostModeratedEvent(actor.Value, GroupId, post.Id, (int)post.State),
                ct
            )
            .ConfigureAwait(false);

        return BuildPostSnapshot(post, 0, 0, DateTime.UtcNow);
    }

    public async Task<ForumSnapshot?> UpdateSettingsAsync(
        PlayerId actor,
        int readPermission,
        int postMessagePermission,
        int postThreadPermission,
        int moderatePermission,
        CancellationToken ct
    )
    {
        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        GroupEntity? group = await dbCtx
            .Groups.Include(g => g.ForumSettings)
            .FirstOrDefaultAsync(g => g.Id == GroupId && g.DeletedAt == null, ct);
        if (group is null || group.ForumSettings is null)
        {
            return null;
        }

        // Forum settings are owner-only.
        if (group.OwnerPlayerEntityId != actor.Value)
        {
            return null;
        }

        GroupForumSettingsEntity? settings = group.ForumSettings;
        settings.Enabled = true;
        settings.ReadPermission = ClampPermission(readPermission);
        settings.PostPermission = ClampPermission(postMessagePermission);
        settings.ThreadPermission = ClampPermission(postThreadPermission);
        settings.ModPermission = ClampPermission(moderatePermission);

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        await events
            .PublishAsync(new ForumSettingsUpdatedEvent(actor.Value, GroupId), ct)
            .ConfigureAwait(false);

        return await BuildForumSnapshotAsync(dbCtx, group, settings, ForumRole.Owner, ct);
    }

    private async Task<ForumRole> GetRoleAsync(
        TurboDbContext dbCtx,
        GroupEntity group,
        int playerId,
        CancellationToken ct
    )
    {
        if (group.OwnerPlayerEntityId == playerId)
        {
            return ForumRole.Owner;
        }

        GroupMemberEntity? membership = await dbCtx
            .GroupMembers.AsNoTracking()
            .FirstOrDefaultAsync(
                m =>
                    m.GroupEntityId == group.Id
                    && m.PlayerEntityId == playerId
                    && m.DeletedAt == null,
                ct
            );

        return membership is null ? ForumRole.None
            : membership.Rank == GroupMemberRank.Admin ? ForumRole.Admin
            : ForumRole.Member;
    }

    private static bool Allows(GroupForumPermission required, ForumRole role) =>
        required switch
        {
            GroupForumPermission.Everyone => true,
            GroupForumPermission.Members => role >= ForumRole.Member,
            GroupForumPermission.Admins => role >= ForumRole.Admin,
            GroupForumPermission.Owner => role == ForumRole.Owner,
            _ => false,
        };

    private static bool CanRead(GroupForumSettingsEntity settings, ForumRole role) =>
        settings.Enabled ? Allows(settings.ReadPermission, role) : role == ForumRole.Owner;

    private async Task<(GroupEntity? group, ForumRole role)> LoadForModerationAsync(
        TurboDbContext dbCtx,
        PlayerId actor,
        CancellationToken ct
    )
    {
        GroupEntity? group = await dbCtx
            .Groups.Include(g => g.ForumSettings)
            .FirstOrDefaultAsync(g => g.Id == GroupId && g.DeletedAt == null, ct);
        if (group is null || group.ForumSettings is null)
        {
            return (null, ForumRole.None);
        }

        ForumRole role = await GetRoleAsync(dbCtx, group, actor.Value, ct);
        return Allows(group.ForumSettings.ModPermission, role)
            ? (group, role)
            : (null, ForumRole.None);
    }

    private async Task<ForumSnapshot> BuildForumSnapshotAsync(
        TurboDbContext dbCtx,
        GroupEntity group,
        GroupForumSettingsEntity settings,
        ForumRole role,
        CancellationToken ct
    )
    {
        int totalThreads = await dbCtx.GroupForumThreads.CountAsync(
            t =>
                t.GroupEntityId == GroupId
                && t.DeletedAt == null
                && t.State != GroupForumThreadState.Hidden,
            ct
        );

        int totalMessages = await dbCtx.GroupForumPosts.CountAsync(
            p =>
                p.GroupEntityId == GroupId
                && p.DeletedAt == null
                && p.State == GroupForumPostState.Visible,
            ct
        );

        GroupForumPostEntity? lastPost = await dbCtx
            .GroupForumPosts.AsNoTracking()
            .Include(p => p.PlayerEntity)
            .Where(p =>
                p.GroupEntityId == GroupId
                && p.DeletedAt == null
                && p.State == GroupForumPostState.Visible
            )
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync(ct);

        DateTime now = DateTime.UtcNow;
        bool canRead = CanRead(settings, role);
        bool canPostMessage = settings.Enabled && Allows(settings.PostPermission, role);
        bool canPostThread = settings.Enabled && Allows(settings.ThreadPermission, role);
        bool canModerate = Allows(settings.ModPermission, role);

        return new ForumSnapshot
        {
            GroupId = GroupId,
            Name = group.Name,
            Description = group.Description ?? string.Empty,
            Icon = group.Badge,
            TotalThreads = totalThreads,
            LeaderboardScore = 0,
            TotalMessages = totalMessages,
            UnreadMessages = 0,
            LastMessageId = lastPost?.Id ?? 0,
            LastMessageAuthorId = lastPost?.PlayerEntityId ?? 0,
            LastMessageAuthorName = lastPost?.PlayerEntity.Name ?? string.Empty,
            LastMessageTimeAsSecondsAgo = lastPost is null
                ? 0
                : SecondsAgo(lastPost.CreatedAt, now),
            ReadPermissions = (int)settings.ReadPermission,
            PostMessagePermissions = (int)settings.PostPermission,
            PostThreadPermissions = (int)settings.ThreadPermission,
            ModeratePermissions = (int)settings.ModPermission,
            ReadPermissionError =
                canRead ? string.Empty
                : settings.Enabled ? DeniedError
                : DisabledError,
            PostMessagePermissionError = canPostMessage ? string.Empty : DeniedError,
            PostThreadPermissionError = canPostThread ? string.Empty : DeniedError,
            ModeratePermissionError = canModerate ? string.Empty : DeniedError,
            ReportPermissionError = canRead ? string.Empty : DeniedError,
            CanChangeSettings = role == ForumRole.Owner,
            IsStaff = false,
        };
    }

    private static ForumThreadSnapshot BuildThreadSnapshot(
        GroupForumThreadEntity thread,
        DateTime now,
        string? authorName = null,
        string? lastPostName = null
    ) =>
        new()
        {
            ThreadId = thread.Id,
            AuthorId = thread.PlayerEntityId,
            AuthorName = authorName ?? thread.PlayerEntity?.Name ?? string.Empty,
            Subject = thread.Subject,
            IsSticky = thread.IsPinned,
            IsLocked = thread.State == GroupForumThreadState.Locked,
            CreationTimeAsSecondsAgo = SecondsAgo(thread.CreatedAt, now),
            MessageCount = thread.PostCount,
            UnreadMessageCount = 0,
            LastMessageId = 0,
            LastMessageAuthorId = thread.LastPostPlayerEntityId ?? 0,
            LastMessageAuthorName =
                lastPostName ?? thread.LastPostPlayerEntity?.Name ?? string.Empty,
            LastMessageTimeAsSecondsAgo = thread.LastPostAt is null
                ? SecondsAgo(thread.CreatedAt, now)
                : SecondsAgo(thread.LastPostAt.Value, now),
            State = (int)thread.State,
            AdminId = 0,
            AdminName = string.Empty,
            AdminOperationTimeAsSecondsAgo = 0,
        };

    private static ForumPostSnapshot BuildPostSnapshot(
        GroupForumPostEntity post,
        int messageIndex,
        int authorPostCount,
        DateTime now,
        Database.Entities.Players.PlayerEntity? author = null
    ) =>
        new()
        {
            MessageId = post.Id,
            MessageIndex = messageIndex,
            AuthorId = post.PlayerEntityId,
            AuthorName = author?.Name ?? post.PlayerEntity?.Name ?? string.Empty,
            AuthorFigure = author?.Figure ?? post.PlayerEntity?.Figure ?? string.Empty,
            CreationTimeAsSecondsAgo = SecondsAgo(post.CreatedAt, now),
            MessageText = post.Message,
            State = (int)post.State,
            AdminId = 0,
            AdminName = string.Empty,
            AdminOperationTimeAsSecondsAgo = 0,
            AuthorPostCount = authorPostCount,
        };

    private async Task<Dictionary<int, int>> GetAuthorPostCountsAsync(
        TurboDbContext dbCtx,
        List<GroupForumPostEntity> posts,
        CancellationToken ct
    )
    {
        if (posts.Count == 0)
        {
            return [];
        }

        List<int> authorIds = posts.Select(p => p.PlayerEntityId).Distinct().ToList();

        return await dbCtx
            .GroupForumPosts.AsNoTracking()
            .Where(p =>
                p.GroupEntityId == GroupId
                && authorIds.Contains(p.PlayerEntityId)
                && p.State == GroupForumPostState.Visible
                && p.DeletedAt == null
            )
            .GroupBy(p => p.PlayerEntityId)
            .Select(g => new { AuthorId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.AuthorId, x => x.Count, ct);
    }

    private static int SecondsAgo(DateTime then, DateTime now) =>
        (int)Math.Max(0, (now - then).TotalSeconds);

    private static int NormalizeAmount(int amount) => amount is <= 0 or > 50 ? 20 : amount;

    private static GroupForumPermission ClampPermission(int value) =>
        value is >= 0 and <= 3 ? (GroupForumPermission)value : GroupForumPermission.Members;

    // Moderation action codes (client forum moderation menu): 20 = hide, 10 = restore/open,
    // anything else defaults to hide. Documented best-effort mapping; states never hard-delete.
    private static GroupForumThreadState MapThreadAction(int action) =>
        action switch
        {
            10 => GroupForumThreadState.Open,
            11 => GroupForumThreadState.Locked,
            _ => GroupForumThreadState.Hidden,
        };

    private static GroupForumPostState MapPostAction(int action) =>
        action switch
        {
            10 => GroupForumPostState.Visible,
            _ => GroupForumPostState.Hidden,
        };
}
