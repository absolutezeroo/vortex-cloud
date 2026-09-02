using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Players;
using Vortex.Primitives.FriendList.Enums;
using Vortex.Primitives.FriendList.Grains;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Snapshots.FriendList;
using Vortex.Protocol.Messages.Outgoing.FriendList;

namespace Vortex.Social.Grains;

internal sealed partial class MessengerGrain
{
    public async Task NotifyOnlineAsync(CancellationToken ct)
    {
        // Fan out to all friends (fire-and-forget) so they update their snapshot for us
        int selfIdInt = SelfId;

        await using VortexDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);
        PlayerEntity? selfEntity = await dbCtx
            .Players.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == selfIdInt, ct);

        if (selfEntity is null)
        {
            return;
        }

        foreach (PlayerId friendId in FriendsWhoCanSeeThis())
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
        await using VortexDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);
        PlayerEntity? selfEntity = await dbCtx
            .Players.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == (int)SelfId, ct);

        if (selfEntity is null)
        {
            return;
        }

        foreach (PlayerId friendId in FriendsWhoCanSeeThis())
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

    /// <summary>
    /// The friends there is any point telling: the ones with a client connected right now.
    /// </summary>
    /// <remarks>
    /// This loop used to run over the whole friend list, and a friend-list entry is not a cheap
    /// thing to call. Addressing a messenger grain activates it, and MessengerGrain.OnActivateAsync
    /// hydrates from the database and registers a repeating timer -- so a player with a full Habbo
    /// list of 1,100 friends cost 1,100 activations, 1,100 queries and 1,100 resident timers every
    /// time they logged in, to deliver a presence update to 1,080 people who were not there to read
    /// it. Five hundred players reconnecting after a restart is that number again five hundred
    /// times, in a few seconds. RoomConfig's own comment records what a login storm did to the room
    /// tick -- "a run during a login storm managed 11 [Hz]" -- and this is the most likely reason.
    /// <para>
    /// The online set is a projection the session layer already keeps in memory, so asking it costs
    /// nothing and activates nobody. It is silo-local, which this build already is everywhere else;
    /// the startup guard in VortexEmulator refuses a second silo and names this among the reasons.
    /// </para>
    /// </remarks>
    private IEnumerable<PlayerId> FriendsWhoCanSeeThis()
    {
        HashSet<PlayerId> online = [.. _sessionGateway.GetOnlinePlayerIds()];

        return _friends.Keys.Where(online.Contains);
    }

    public async Task NotifyFriendPresenceChangedAsync(
        PlayerId friendId,
        bool online,
        string figure,
        string motto,
        CancellationToken ct
    )
    {
        if (!_friends.TryGetValue(friendId, out MessengerFriendSnapshot? existing))
        {
            // New friend added while we were offline -- add to in-memory cache. The notification
            // carries a figure and a motto but no name, and the composer below goes straight to the
            // client, so the name is resolved here rather than left blank; this branch only runs for
            // a friend we have never cached, so the extra lookup stays off the fan-out path.
            string name = await grainFactory
                .GetPlayerDirectoryGrain()
                .GetPlayerNameAsync(friendId, ct)
                .ConfigureAwait(true);

            _friends[friendId] = new MessengerFriendSnapshot
            {
                PlayerId = friendId,
                Name = name,
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
