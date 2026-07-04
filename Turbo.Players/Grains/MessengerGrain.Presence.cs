using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Orleans;
using Turbo.Database.Context;
using Turbo.Database.Entities.Players;
using Turbo.Primitives.FriendList.Enums;
using Turbo.Primitives.FriendList.Grains;
using Turbo.Primitives.Messages.Outgoing.FriendList;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players;
using Turbo.Primitives.Players.Grains;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Primitives.Snapshots.FriendList;

namespace Turbo.Players.Grains;

internal sealed partial class MessengerGrain
{
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
                RelationshipStatus = 0,
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
                            Friend = updated,
                        },
                    ],
                }
            )
        );

        return Task.CompletedTask;
    }

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
}
