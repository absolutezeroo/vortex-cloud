using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Messenger;
using Vortex.Database.Entities.Players;
using Vortex.Logging.Extensions;
using Vortex.Players.Configuration;
using Vortex.Primitives.Events;
using Vortex.Primitives.FriendList.Enums;
using Vortex.Primitives.FriendList.Grains;
using Vortex.Primitives.Messages.Outgoing.FriendList;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Snapshots.FriendList;

namespace Vortex.Players.Grains;

/// <summary>
///     Lifecycle, hydration, and shared fire-and-forget/flush helpers. Friend list/requests/
///     blocking live in <c>MessengerGrain.Friends.cs</c>, instant messages in
///     <c>MessengerGrain.Messaging.cs</c>, online/offline fan-out in
///     <c>MessengerGrain.Presence.cs</c>.
/// </summary>
internal sealed partial class MessengerGrain(
    IDbContextFactory<TurboDbContext> dbCtxFactory,
    IGrainFactory grainFactory,
    IEventPublisher events,
    ILogger<MessengerGrain> logger,
    IOptions<MessengerConfig> messengerConfig
) : Grain, IMessengerGrain
{
    private readonly MessengerConfig _messengerConfig = messengerConfig.Value;
    private readonly HashSet<int> _blockedIds = new();
    private readonly List<FriendCategorySnapshot> _categories = new();

    // In-memory state — grain thread safety guaranteed by Orleans single-threaded activation
    private readonly Dictionary<PlayerId, MessengerFriendSnapshot> _friends = new();
    private readonly HashSet<int> _ignoredIds = new();
    private readonly Dictionary<int, FriendRequestSnapshot> _incomingRequests = new(); // key = requester id
    private readonly HashSet<int> _pendingDeliveredIds = new();

    private PlayerId SelfId => PlayerId.Parse((int)this.GetPrimaryKeyLong());

    public override async Task OnActivateAsync(CancellationToken ct)
    {
        await HydrateAsync(ct);

        TimeSpan flushInterval = TimeSpan.FromMilliseconds(
            _messengerConfig.DeliveredFlushIntervalMs
        );

        this.RegisterGrainTimer<object?>(
            async (_, ct) => await FlushDeliveredMessagesAsync(ct),
            null,
            flushInterval,
            flushInterval
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
                FigureString = r.PlayerEntity.Figure,
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
            RelationshipStatus = (short)entity.RelationType,
        };
    }

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
                .ConfigureAwait(true);
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

    private void LogAndForget(Task task) =>
        task.LogAndForget(
            logger,
            "Unhandled error in fire-and-forget messenger operation for player {PlayerId}",
            SelfId
        );

    private async Task DeliverOfflinePendingMessagesAsync(CancellationToken ct)
    {
        await using TurboDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        List<MessengerMessageEntity> pending = await dbCtx
            .MessengerMessages.AsNoTracking()
            .Include(m => m.SenderEntity)
            .Where(m => m.ReceiverEntityId == (int)SelfId && !m.Delivered && m.DeletedAt == null)
            .OrderBy(m => m.Timestamp)
            .Take(_messengerConfig.MessageHistoryLimit)
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
                    SenderFigure = msg.SenderEntity.Figure,
                }
            );

            _pendingDeliveredIds.Add(msg.Id);
        }
    }
}
