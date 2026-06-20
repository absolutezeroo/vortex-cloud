using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Turbo.Database.Context;
using Turbo.Database.Entities.Messenger;
using Turbo.Database.Entities.Players;
using Turbo.Primitives.Events;
using Turbo.Primitives.FriendList.Enums;
using Turbo.Primitives.FriendList.Grains;
using Turbo.Primitives.Messages.Outgoing.FriendList;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players;
using Turbo.Primitives.Players.Grains;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Primitives.Snapshots.FriendList;

namespace Turbo.Players.Grains;

internal sealed class MessengerGrain(
    IDbContextFactory<TurboDbContext> dbCtxFactory,
    IGrainFactory grainFactory,
    IEventPublisher events,
    ILogger<MessengerGrain> logger
) : Grain, IMessengerGrain
{
    private const int MaxFriends = 300;
    private const int MessageHistoryLimit = 50;
    private readonly HashSet<int> _blockedIds = new();
    private readonly List<FriendCategorySnapshot> _categories = new();

    // In-memory state — grain thread safety guaranteed by Orleans single-threaded activation
    private readonly Dictionary<PlayerId, MessengerFriendSnapshot> _friends = new();
    private readonly HashSet<int> _ignoredIds = new();
    private readonly Dictionary<int, FriendRequestSnapshot> _incomingRequests = new(); // key = requester id
    private readonly HashSet<int> _pendingDeliveredIds = new();

    private PlayerId SelfId => PlayerId.Parse((int)this.GetPrimaryKeyLong());

    // ── Reads ────────────────────────────────────────────────────────────────

    public Task<List<MessengerFriendSnapshot>> GetFriendsAsync(CancellationToken ct)
    {
        return Task.FromResult(_friends.Values.ToList());
    }

    public Task<List<FriendCategorySnapshot>> GetCategoriesAsync(CancellationToken ct)
    {
        return Task.FromResult(new List<FriendCategorySnapshot>(_categories));
    }

    public Task<List<int>> GetBlockedUserIdsAsync(CancellationToken ct)
    {
        return Task.FromResult(_blockedIds.ToList());
    }

    public Task<List<int>> GetIgnoredUserIdsAsync(CancellationToken ct)
    {
        return Task.FromResult(_ignoredIds.ToList());
    }

    public Task<List<FriendRequestSnapshot>> GetFriendRequestsAsync(CancellationToken ct)
    {
        return Task.FromResult(_incomingRequests.Values.ToList());
    }

    public Task<List<RelationshipStatusEntrySnapshot>> GetRelationshipStatusSummaryAsync(
        CancellationToken ct
    )
    {
        List<RelationshipStatusEntrySnapshot> grouped = _friends
            .Values.Where(f => f.RelationshipStatus != 0)
            .GroupBy(f => f.RelationshipStatus)
            .Select(g =>
            {
                MessengerFriendSnapshot first = g.First();
                return new RelationshipStatusEntrySnapshot
                {
                    RelationType = g.Key,
                    Count = g.Count(),
                    Name = first.Name,
                    Figure = first.Figure
                };
            })
            .ToList();

        return Task.FromResult(grouped);
    }

    public Task<bool> IsFriendAsync(PlayerId targetId, CancellationToken ct)
    {
        return Task.FromResult(_friends.ContainsKey(targetId));
    }

    public Task<bool> HasBlockedUserAsync(PlayerId targetId, CancellationToken ct)
    {
        return Task.FromResult(_blockedIds.Contains(targetId.Value));
    }

    public Task<bool> HasIgnoredUserAsync(PlayerId targetId, CancellationToken ct)
    {
        return Task.FromResult(_ignoredIds.Contains(targetId.Value));
    }

    // ── Friend requests ──────────────────────────────────────────────────────

    public async Task<FriendListErrorCodeType?> SendFriendRequestAsync(
        PlayerId targetId,
        string targetName,
        CancellationToken ct
    )
    {
        if (_friends.Count >= MaxFriends)
        {
            return FriendListErrorCodeType.YouHitFriendLimit;
        }

        if (_friends.ContainsKey(targetId))
        {
            return null; // already friends, not an error per se
        }

        int playerId = SelfId;
        int targetIdInt = targetId;

        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        int targetCountFriends = await dbCtx.MessengerFriends.CountAsync(
            f => f.PlayerEntityId == targetIdInt && f.DeletedAt == null,
            ct
        );

        if (targetCountFriends >= MaxFriends)
        {
            return FriendListErrorCodeType.TheyHitFriendLimit;
        }

        bool alreadyExists = await dbCtx.MessengerRequests.AnyAsync(
            r =>
                r.PlayerEntityId == playerId
                && r.RequestedPlayerEntityId == targetIdInt
                && r.DeletedAt == null,
            ct
        );

        if (alreadyExists)
        {
            return null;
        }

        PlayerEntity selfEntity =
            await dbCtx.Players.FindAsync([playerId], ct)
            ?? throw new InvalidOperationException($"Player {playerId} not found");

        dbCtx.MessengerRequests.Add(
            new MessengerRequestEntity
            {
                PlayerEntityId = playerId,
                RequestedPlayerEntityId = targetIdInt,
                PlayerEntity = selfEntity,
                RequestedPlayerEntity = null!
            }
        );

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        IMessengerGrain targetGrain = grainFactory.GetMessengerGrain(targetId);
        LogAndForget(
            targetGrain.ReceiveFriendRequestAsync(
                SelfId,
                selfEntity.Name,
                selfEntity.Figure,
                CancellationToken.None
            )
        );

        return null;
    }

    public Task ReceiveFriendRequestAsync(
        PlayerId requesterId,
        string requesterName,
        string requesterFigure,
        CancellationToken ct
    )
    {
        _incomingRequests[requesterId.Value] = new FriendRequestSnapshot
        {
            RequestId = requesterId.Value,
            RequesterUserId = requesterId,
            RequesterName = requesterName,
            FigureString = requesterFigure
        };

        IPlayerPresenceGrain presence = grainFactory.GetPlayerPresenceGrain(SelfId);
        LogAndForget(
            presence.SendComposerAsync(
                new NewFriendRequestMessageComposer
                {
                    Request = _incomingRequests[requesterId.Value]
                }
            )
        );

        return Task.CompletedTask;
    }

    public async Task<List<AcceptFriendFailureSnapshot>> AcceptFriendRequestsAsync(
        List<int> requesterIds,
        CancellationToken ct
    )
    {
        List<AcceptFriendFailureSnapshot> failures = new();

        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        foreach (int requesterId in requesterIds)
        {
            if (
                !_incomingRequests.TryGetValue(
                    requesterId,
                    out FriendRequestSnapshot? requestSnapshot
                )
            )
            {
                failures.Add(
                    new AcceptFriendFailureSnapshot
                    {
                        SenderId = PlayerId.Parse(requesterId),
                        ErrorCode = FriendListErrorCodeType.FriendRequestNotFound
                    }
                );
                continue;
            }

            if (_friends.Count >= MaxFriends)
            {
                failures.Add(
                    new AcceptFriendFailureSnapshot
                    {
                        SenderId = PlayerId.Parse(requesterId),
                        ErrorCode = FriendListErrorCodeType.YouHitFriendLimit
                    }
                );
                continue;
            }

            PlayerEntity? selfEntity = await dbCtx.Players.FindAsync([(int)SelfId], ct);
            PlayerEntity? friendEntity = await dbCtx.Players.FindAsync([requesterId], ct);

            if (selfEntity is null || friendEntity is null)
            {
                continue;
            }

            // Remove pending request
            await dbCtx
                .MessengerRequests.Where(r =>
                    r.PlayerEntityId == requesterId
                    && r.RequestedPlayerEntityId == (int)SelfId
                    && r.DeletedAt == null
                )
                .ExecuteDeleteAsync(ct);

            // Create bidirectional friendship
            dbCtx.MessengerFriends.Add(
                new MessengerFriendEntity
                {
                    PlayerEntityId = SelfId,
                    FriendPlayerEntityId = requesterId,
                    RelationType = MessengerFriendRelationType.Zero,
                    PlayerEntity = selfEntity,
                    FriendPlayerEntity = friendEntity
                }
            );

            dbCtx.MessengerFriends.Add(
                new MessengerFriendEntity
                {
                    PlayerEntityId = requesterId,
                    FriendPlayerEntityId = SelfId,
                    RelationType = MessengerFriendRelationType.Zero,
                    PlayerEntity = friendEntity,
                    FriendPlayerEntity = selfEntity
                }
            );

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

            await events
                .PublishAsync(new FriendRequestAcceptedEvent(SelfId, requesterId), ct)
                .ConfigureAwait(false);

            // Update self in-memory
            _incomingRequests.Remove(requesterId);
            _friends[PlayerId.Parse(requesterId)] = new MessengerFriendSnapshot
            {
                PlayerId = PlayerId.Parse(requesterId),
                Name = friendEntity.Name,
                Gender = friendEntity.Gender,
                Online = false,
                FollowingAllowed = true,
                Figure = friendEntity.Figure,
                CategoryId = 0,
                Motto = friendEntity.Motto ?? string.Empty,
                RealName = string.Empty,
                FacebookId = string.Empty,
                PersistedMessageUser = false,
                VipMember = false,
                PocketHabboUser = false,
                RelationshipStatus = 0
            };

            // Notify friend's grain — fire-and-forget
            IMessengerGrain friendGrain = grainFactory.GetMessengerGrain(
                PlayerId.Parse(requesterId)
            );
            LogAndForget(
                friendGrain.NotifyFriendPresenceChangedAsync(
                    SelfId,
                    false,
                    selfEntity.Figure,
                    selfEntity.Motto ?? string.Empty,
                    CancellationToken.None
                )
            );
        }

        return failures;
    }

    public async Task DeclineFriendRequestsAsync(
        List<int> requesterIds,
        bool all,
        CancellationToken ct
    )
    {
        if (all)
        {
            _incomingRequests.Clear();

            await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);
            await dbCtx
                .MessengerRequests.Where(r =>
                    r.RequestedPlayerEntityId == (int)SelfId && r.DeletedAt == null
                )
                .ExecuteDeleteAsync(ct);
            return;
        }

        await using TurboDbContext dbCtxScoped = await dbCtxFactory.CreateDbContextAsync(ct);

        foreach (int requesterId in requesterIds)
        {
            if (!_incomingRequests.Remove(requesterId))
            {
                continue;
            }

            await dbCtxScoped
                .MessengerRequests.Where(r =>
                    r.PlayerEntityId == requesterId
                    && r.RequestedPlayerEntityId == (int)SelfId
                    && r.DeletedAt == null
                )
                .ExecuteDeleteAsync(ct);
        }
    }

    // ── Friendship mutations ─────────────────────────────────────────────────

    public async Task<List<int>> RemoveFriendsAsync(List<int> friendIds, CancellationToken ct)
    {
        List<int> removed = new();

        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        foreach (int friendId in friendIds)
        {
            PlayerId friendKey = PlayerId.Parse(friendId);
            if (!_friends.Remove(friendKey))
            {
                continue;
            }

            // Remove both directions
            await dbCtx
                .MessengerFriends.Where(f =>
                    (f.PlayerEntityId == (int)SelfId && f.FriendPlayerEntityId == friendId)
                    || (f.PlayerEntityId == friendId && f.FriendPlayerEntityId == (int)SelfId)
                )
                .ExecuteDeleteAsync(ct);

            removed.Add(friendId);

            await events
                .PublishAsync(new FriendRemovedEvent(SelfId, friendId), ct)
                .ConfigureAwait(false);

            // Notify friend's grain — fire-and-forget
            IMessengerGrain friendGrain = grainFactory.GetMessengerGrain(friendKey);
            LogAndForget(friendGrain.NotifyFriendRemovedAsync(SelfId, CancellationToken.None));
        }

        return removed;
    }

    public Task NotifyFriendRemovedAsync(PlayerId removedBy, CancellationToken ct)
    {
        _friends.Remove(removedBy);

        IPlayerPresenceGrain presence = grainFactory.GetPlayerPresenceGrain(SelfId);
        LogAndForget(
            presence.SendComposerAsync(
                new FriendListUpdateMessageComposer
                {
                    FriendCategories = [],
                    Updates =
                    [
                        new FriendListUpdateSnapshot
                        {
                            ActionType = FriendListUpdateActionType.Removed,
                            FriendId = removedBy.Value,
                            Friend = null
                        }
                    ]
                }
            )
        );

        return Task.CompletedTask;
    }

    public async Task SetRelationshipStatusAsync(
        int friendId,
        MessengerFriendRelationType relationType,
        CancellationToken ct
    )
    {
        PlayerId friendKey = PlayerId.Parse(friendId);
        if (!_friends.TryGetValue(friendKey, out MessengerFriendSnapshot? existing))
        {
            return;
        }

        _friends[friendKey] = existing with { RelationshipStatus = (short)relationType };

        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);
        await dbCtx
            .MessengerFriends.Where(f =>
                f.PlayerEntityId == (int)SelfId
                && f.FriendPlayerEntityId == friendId
                && f.DeletedAt == null
            )
            .ExecuteUpdateAsync(up => up.SetProperty(p => p.RelationType, relationType), ct)
            .ConfigureAwait(false);
    }

    // ── Block / ignore ───────────────────────────────────────────────────────

    public async Task BlockUserAsync(PlayerId targetId, CancellationToken ct)
    {
        if (_blockedIds.Contains(targetId.Value))
        {
            return;
        }

        _blockedIds.Add(targetId.Value);

        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        PlayerEntity? selfEntity = await dbCtx.Players.FindAsync([(int)SelfId], ct);
        PlayerEntity? targetEntity = await dbCtx.Players.FindAsync([targetId.Value], ct);

        if (selfEntity is null || targetEntity is null)
        {
            return;
        }

        dbCtx.MessengerBlocked.Add(
            new MessengerBlockedEntity
            {
                PlayerEntityId = SelfId,
                BlockedPlayerEntityId = targetId.Value,
                PlayerEntity = selfEntity,
                BlockedPlayerEntity = targetEntity
            }
        );

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        await events
            .PublishAsync(new UserBlockedEvent(SelfId, targetId.Value), ct)
            .ConfigureAwait(false);
    }

    public async Task UnblockUserAsync(PlayerId targetId, CancellationToken ct)
    {
        if (!_blockedIds.Remove(targetId.Value))
        {
            return;
        }

        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        await dbCtx
            .MessengerBlocked.Where(b =>
                b.PlayerEntityId == (int)SelfId
                && b.BlockedPlayerEntityId == targetId.Value
                && b.DeletedAt == null
            )
            .ExecuteDeleteAsync(ct);

        await events
            .PublishAsync(new UserUnblockedEvent(SelfId, targetId.Value), ct)
            .ConfigureAwait(false);
    }

    public async Task IgnoreUserAsync(PlayerId targetId, CancellationToken ct)
    {
        if (_ignoredIds.Contains(targetId.Value))
        {
            return;
        }

        _ignoredIds.Add(targetId.Value);

        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        PlayerEntity? selfEntity = await dbCtx.Players.FindAsync([(int)SelfId], ct);
        PlayerEntity? targetEntity = await dbCtx.Players.FindAsync([targetId.Value], ct);

        if (selfEntity is null || targetEntity is null)
        {
            return;
        }

        dbCtx.MessengerIgnored.Add(
            new MessengerIgnoredEntity
            {
                PlayerEntityId = SelfId,
                IgnoredPlayerEntityId = targetId.Value,
                PlayerEntity = selfEntity,
                IgnoredPlayerEntity = targetEntity
            }
        );

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UnignoreUserAsync(PlayerId targetId, CancellationToken ct)
    {
        if (!_ignoredIds.Remove(targetId.Value))
        {
            return;
        }

        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        await dbCtx
            .MessengerIgnored.Where(i =>
                i.PlayerEntityId == (int)SelfId
                && i.IgnoredPlayerEntityId == targetId.Value
                && i.DeletedAt == null
            )
            .ExecuteDeleteAsync(ct);
    }

    // ── Messaging ────────────────────────────────────────────────────────────

    public async Task<InstantMessageErrorCodeType?> SendMessageAsync(
        PlayerId receiverId,
        string message,
        int chatId,
        int confirmationId,
        CancellationToken ct
    )
    {
        if (!_friends.ContainsKey(receiverId))
        {
            return InstantMessageErrorCodeType.NotFriend;
        }

        if (_blockedIds.Contains(receiverId.Value))
        {
            return InstantMessageErrorCodeType.ReceiverMuted;
        }

        DateTime now = DateTime.UtcNow;

        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        PlayerEntity? selfEntity = await dbCtx.Players.FindAsync([(int)SelfId], ct);
        PlayerEntity? receiverEntity = await dbCtx.Players.FindAsync([receiverId.Value], ct);

        if (selfEntity is null || receiverEntity is null)
        {
            return InstantMessageErrorCodeType.Offline;
        }

        MessengerMessageEntity msgEntity = new()
        {
            SenderEntityId = SelfId,
            ReceiverEntityId = receiverId.Value,
            Message = message,
            Timestamp = now,
            Delivered = false,
            SenderEntity = selfEntity,
            ReceiverEntity = receiverEntity
        };

        dbCtx.MessengerMessages.Add(msgEntity);
        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        // Deliver to receiver — fire-and-forget; receiver handles if online or not
        IMessengerGrain receiverGrain = grainFactory.GetMessengerGrain(receiverId);
        LogAndForget(
            receiverGrain.ReceiveMessageAsync(
                SelfId,
                selfEntity.Name,
                selfEntity.Figure,
                message,
                now,
                msgEntity.Id,
                CancellationToken.None
            )
        );

        return null;
    }

    public Task ReceiveMessageAsync(
        PlayerId senderId,
        string senderName,
        string senderFigure,
        string message,
        DateTime timestamp,
        int messageId,
        CancellationToken ct
    )
    {
        if (_ignoredIds.Contains(senderId.Value))
        {
            return Task.CompletedTask;
        }

        int secondsSinceSent = (int)(DateTime.UtcNow - timestamp).TotalSeconds;

        IPlayerPresenceGrain presence = grainFactory.GetPlayerPresenceGrain(SelfId);
        LogAndForget(
            presence.SendComposerAsync(
                new NewConsoleMessageMessageComposer
                {
                    ChatId = senderId.Value,
                    Message = message,
                    SecondsSinceSent = secondsSinceSent,
                    MessageId = messageId.ToString(),
                    ConfirmationId = 0,
                    SenderId = senderId.Value,
                    SenderName = senderName,
                    SenderFigure = senderFigure
                }
            )
        );

        _pendingDeliveredIds.Add(messageId);

        return Task.CompletedTask;
    }

    public async Task<List<MessageHistoryEntrySnapshot>> GetMessageHistoryAsync(
        PlayerId friendId,
        CancellationToken ct
    )
    {
        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        int selfIdInt = SelfId;
        int friendIdInt = friendId.Value;
        DateTime now = DateTime.UtcNow;

        List<MessengerMessageEntity> messages = await dbCtx
            .MessengerMessages.AsNoTracking()
            .Include(m => m.SenderEntity)
            .Where(m =>
                m.DeletedAt == null
                && (
                    (m.SenderEntityId == selfIdInt && m.ReceiverEntityId == friendIdInt)
                    || (m.SenderEntityId == friendIdInt && m.ReceiverEntityId == selfIdInt)
                )
            )
            .OrderByDescending(m => m.Timestamp)
            .Take(MessageHistoryLimit)
            .ToListAsync(ct);

        return messages
            .Select(m => new MessageHistoryEntrySnapshot
            {
                SenderId = m.SenderEntityId,
                SenderName = m.SenderEntity.Name,
                SenderFigure = m.SenderEntity.Figure,
                Message = m.Message,
                SecondsSinceSent = (int)(now - m.Timestamp).TotalSeconds,
                MessageId = m.Id.ToString()
            })
            .ToList();
    }

    // ── Presence notifications ───────────────────────────────────────────────

    public async Task NotifyOnlineAsync(CancellationToken ct)
    {
        // Fan out to all friends (fire-and-forget) so they update their snapshot for us
        int selfIdInt = SelfId;

        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);
        PlayerEntity? selfEntity = await dbCtx
            .Players.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == selfIdInt, ct);

        if (selfEntity is null)
        {
            return;
        }

        foreach ((PlayerId friendId, MessengerFriendSnapshot _) in _friends)
        {
            IMessengerGrain friendGrain = grainFactory.GetMessengerGrain(friendId);
            LogAndForget(
                friendGrain.NotifyFriendPresenceChangedAsync(
                    SelfId,
                    true,
                    selfEntity.Figure,
                    selfEntity.Motto ?? string.Empty,
                    CancellationToken.None
                )
            );
        }

        // Deliver unread offline messages to self
        await DeliverOfflinePendingMessagesAsync(ct);
    }

    public async Task NotifyOfflineAsync(CancellationToken ct)
    {
        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);
        PlayerEntity? selfEntity = await dbCtx
            .Players.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == (int)SelfId, ct);

        if (selfEntity is null)
        {
            return;
        }

        foreach ((PlayerId friendId, MessengerFriendSnapshot _) in _friends)
        {
            IMessengerGrain friendGrain = grainFactory.GetMessengerGrain(friendId);
            LogAndForget(
                friendGrain.NotifyFriendPresenceChangedAsync(
                    SelfId,
                    false,
                    selfEntity.Figure,
                    selfEntity.Motto ?? string.Empty,
                    CancellationToken.None
                )
            );
        }
    }

    public Task NotifyFriendPresenceChangedAsync(
        PlayerId friendId,
        bool online,
        string figure,
        string motto,
        CancellationToken ct
    )
    {
        if (!_friends.TryGetValue(friendId, out MessengerFriendSnapshot? existing))
        {
            // New friend added while we were offline — add to in-memory cache
            _friends[friendId] = new MessengerFriendSnapshot
            {
                PlayerId = friendId,
                Name = string.Empty,
                Gender = AvatarGenderType.Male,
                Online = online,
                FollowingAllowed = true,
                Figure = figure,
                CategoryId = 0,
                Motto = motto,
                RealName = string.Empty,
                FacebookId = string.Empty,
                PersistedMessageUser = false,
                VipMember = false,
                PocketHabboUser = false,
                RelationshipStatus = 0
            };
        }
        else
        {
            _friends[friendId] = existing with { Online = online, Figure = figure, Motto = motto };
        }

        MessengerFriendSnapshot updated = _friends[friendId];
        IPlayerPresenceGrain presence = grainFactory.GetPlayerPresenceGrain(SelfId);
        LogAndForget(
            presence.SendComposerAsync(
                new FriendListUpdateMessageComposer
                {
                    FriendCategories = [],
                    Updates =
                    [
                        new FriendListUpdateSnapshot
                        {
                            ActionType = FriendListUpdateActionType.Updated,
                            FriendId = friendId.Value,
                            Friend = updated
                        }
                    ]
                }
            )
        );

        return Task.CompletedTask;
    }

    // ── Room actions ─────────────────────────────────────────────────────────

    public Task ReceiveRoomInviteAsync(PlayerId senderId, string message, CancellationToken ct)
    {
        if (_ignoredIds.Contains(senderId.Value))
        {
            return Task.CompletedTask;
        }

        IPlayerPresenceGrain presence = grainFactory.GetPlayerPresenceGrain(SelfId);
        LogAndForget(
            presence.SendComposerAsync(
                new RoomInviteMessageComposer { SenderId = senderId.Value, Message = message }
            )
        );

        return Task.CompletedTask;
    }

    // ── Search ───────────────────────────────────────────────────────────────

    public Task<List<MessengerSearchResultSnapshot>> GetFriendSearchResultsAsync(
        string query,
        CancellationToken ct
    )
    {
        string lower = query.ToLowerInvariant();

        List<MessengerSearchResultSnapshot> results = _friends
            .Values.Where(f => f.Name.Contains(lower, StringComparison.OrdinalIgnoreCase))
            .Select(f => new MessengerSearchResultSnapshot
            {
                PlayerId = f.PlayerId,
                Name = f.Name,
                Motto = f.Motto,
                Online = f.Online,
                FollowingAllowed = f.FollowingAllowed,
                UnknownString = string.Empty,
                Gender = f.Gender,
                Figure = f.Figure,
                RealName = f.RealName
            })
            .ToList();

        return Task.FromResult(results);
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override async Task OnActivateAsync(CancellationToken ct)
    {
        await HydrateAsync(ct);

        this.RegisterGrainTimer<object?>(
            async (_, ct) => await FlushDeliveredMessagesAsync(ct),
            null,
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(10)
        );
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken ct)
    {
        if (_pendingDeliveredIds.Count > 0)
        {
            await FlushDeliveredMessagesAsync(ct);
        }
    }

    // ── Hydration ────────────────────────────────────────────────────────────

    private async Task HydrateAsync(CancellationToken ct)
    {
        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        int playerId = SelfId;

        List<MessengerCategoryEntity> categories = await dbCtx
            .MessengerCategories.AsNoTracking()
            .Where(c => c.PlayerEntityId == playerId && c.DeletedAt == null)
            .ToListAsync(ct);

        foreach (MessengerCategoryEntity cat in categories)
        {
            _categories.Add(new FriendCategorySnapshot { Id = cat.Id, Name = cat.Name });
        }

        List<MessengerFriendEntity> friends = await dbCtx
            .MessengerFriends.AsNoTracking()
            .Include(f => f.FriendPlayerEntity)
            .Where(f => f.PlayerEntityId == playerId && f.DeletedAt == null)
            .ToListAsync(ct);

        foreach (MessengerFriendEntity f in friends)
        {
            MessengerFriendSnapshot snapshot = BuildFriendSnapshot(f, false);
            _friends[snapshot.PlayerId] = snapshot;
        }

        List<MessengerRequestEntity> requests = await dbCtx
            .MessengerRequests.AsNoTracking()
            .Include(r => r.PlayerEntity)
            .Where(r => r.RequestedPlayerEntityId == playerId && r.DeletedAt == null)
            .ToListAsync(ct);

        foreach (MessengerRequestEntity r in requests)
        {
            _incomingRequests[r.PlayerEntityId] = new FriendRequestSnapshot
            {
                RequestId = r.Id,
                RequesterUserId = PlayerId.Parse(r.PlayerEntityId),
                RequesterName = r.PlayerEntity.Name,
                FigureString = r.PlayerEntity.Figure
            };
        }

        List<int> blocked = await dbCtx
            .MessengerBlocked.AsNoTracking()
            .Where(b => b.PlayerEntityId == playerId && b.DeletedAt == null)
            .Select(b => b.BlockedPlayerEntityId)
            .ToListAsync(ct);

        foreach (int id in blocked)
        {
            _blockedIds.Add(id);
        }

        List<int> ignored = await dbCtx
            .MessengerIgnored.AsNoTracking()
            .Where(i => i.PlayerEntityId == playerId && i.DeletedAt == null)
            .Select(i => i.IgnoredPlayerEntityId)
            .ToListAsync(ct);

        foreach (int id in ignored)
        {
            _ignoredIds.Add(id);
        }
    }

    private static MessengerFriendSnapshot BuildFriendSnapshot(
        MessengerFriendEntity entity,
        bool online
    )
    {
        return new MessengerFriendSnapshot
        {
            PlayerId = PlayerId.Parse(entity.FriendPlayerEntityId),
            Name = entity.FriendPlayerEntity.Name,
            Gender = entity.FriendPlayerEntity.Gender,
            Online = online,
            FollowingAllowed = true,
            Figure = entity.FriendPlayerEntity.Figure,
            CategoryId = entity.MessengerCategoryEntityId ?? 0,
            Motto = entity.FriendPlayerEntity.Motto ?? string.Empty,
            RealName = string.Empty,
            FacebookId = string.Empty,
            PersistedMessageUser = false,
            VipMember = false,
            PocketHabboUser = false,
            RelationshipStatus = (short)entity.RelationType
        };
    }

    // ── Timer ────────────────────────────────────────────────────────────────

    private async Task FlushDeliveredMessagesAsync(CancellationToken ct)
    {
        if (_pendingDeliveredIds.Count == 0)
        {
            return;
        }

        List<int> ids = _pendingDeliveredIds.ToList();
        _pendingDeliveredIds.Clear();

        try
        {
            await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

            await dbCtx
                .MessengerMessages.Where(m => ids.Contains(m.Id))
                .ExecuteUpdateAsync(up => up.SetProperty(p => p.Delivered, true), ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to flush delivered messenger message IDs for player {PlayerId}",
                SelfId
            );
        }
    }

    // ── Fire-and-forget ───────────────────────────────────────────────────────

    private void LogAndForget(Task task)
    {
        task.ContinueWith(
            t =>
                logger.LogError(
                    t.Exception,
                    "Unhandled error in fire-and-forget messenger operation for player {PlayerId}",
                    SelfId
                ),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Current
        );
    }

    private async Task DeliverOfflinePendingMessagesAsync(CancellationToken ct)
    {
        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        List<MessengerMessageEntity> pending = await dbCtx
            .MessengerMessages.AsNoTracking()
            .Include(m => m.SenderEntity)
            .Where(m => m.ReceiverEntityId == (int)SelfId && !m.Delivered && m.DeletedAt == null)
            .OrderBy(m => m.Timestamp)
            .Take(50)
            .ToListAsync(ct);

        if (pending.Count == 0)
        {
            return;
        }

        IPlayerPresenceGrain presence = grainFactory.GetPlayerPresenceGrain(SelfId);
        DateTime now = DateTime.UtcNow;

        foreach (MessengerMessageEntity msg in pending)
        {
            if (_ignoredIds.Contains(msg.SenderEntityId))
            {
                continue;
            }

            await presence.SendComposerAsync(
                new NewConsoleMessageMessageComposer
                {
                    ChatId = msg.SenderEntityId,
                    Message = msg.Message,
                    SecondsSinceSent = (int)(now - msg.Timestamp).TotalSeconds,
                    MessageId = msg.Id.ToString(),
                    ConfirmationId = 0,
                    SenderId = msg.SenderEntityId,
                    SenderName = msg.SenderEntity.Name,
                    SenderFigure = msg.SenderEntity.Figure
                }
            );

            _pendingDeliveredIds.Add(msg.Id);
        }
    }
}
