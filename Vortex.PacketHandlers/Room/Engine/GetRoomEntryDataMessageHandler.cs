using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Action;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Primitives.Rooms.Snapshots.Avatars;
using Vortex.Primitives.Rooms.Snapshots.Furniture;
using Vortex.Protocol.Messages.Incoming.Room.Engine;
using Vortex.Protocol.Messages.Outgoing.Room.Action;
using Vortex.Protocol.Messages.Outgoing.Room.Engine;
using Vortex.Protocol.Messages.Outgoing.Room.Permissions;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;

namespace Vortex.PacketHandlers.Room.Engine;

public class GetRoomEntryDataMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetRoomEntryDataMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetRoomEntryDataMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        IPlayerPresenceGrain playerPresence = _grainFactory.GetPlayerPresenceGrain(ctx.PlayerId);
        RoomPendingSnapshot pendingRoom = await playerPresence
            .GetPendingRoomAsync()
            .ConfigureAwait(false);
        RoomId roomId = pendingRoom.RoomId;

        if (roomId <= 0)
        {
            return;
        }

        // The entry payload genuinely spans three facets of the room, so it takes one reference to
        // each rather than the whole IRoomGrain. All three resolve to the same activation.
        IRoomFurni roomFurni = _grainFactory.GetRoomFurni(roomId);
        IRoomAvatars roomAvatars = _grainFactory.GetRoomAvatars(roomId);

        ActionContext actionCtx = ActionContext.CreateForPlayer(ctx.PlayerId, roomId);
        RoomControllerType controllerLevel = await _grainFactory
            .GetRoomSecurity(roomId)
            .GetControllerLevelAsync(actionCtx, ct)
            .ConfigureAwait(false);
        // Moderator outranks Owner in this ladder, so an equality test excluded staff from the
        // owner-facing entry data while PlayerPresenceGrain already sent them YouAreOwner. The two
        // sites contradicted each other; both are >= now.
        bool isOwner = controllerLevel >= RoomControllerType.Owner;
        bool hasRights = controllerLevel >= RoomControllerType.Rights;

        ImmutableDictionary<PlayerId, string> ownersSnapshot = await roomFurni
            .GetAllOwnersAsync(ct)
            .ConfigureAwait(false);
        ImmutableArray<RoomFloorItemSnapshot> floorSnapshot = await roomFurni
            .GetAllFloorItemSnapshotsAsync(ct)
            .ConfigureAwait(false);
        ImmutableArray<RoomWallItemSnapshot> wallSnapshot = await roomFurni
            .GetAllWallItemSnapshotsAsync(ct)
            .ConfigureAwait(false);
        ImmutableArray<RoomAvatarSnapshot> avatarSnapshots = await roomAvatars
            .GetAllAvatarSnapshotsAsync(ct)
            .ConfigureAwait(false);

        // Bots dance too, and their dance is as absent from the Users payload as a player's, so
        // both kinds are replayed here rather than only the ones with an account.
        IComposer[] danceComposers =
        [
            .. avatarSnapshots
                .OfType<RoomPlayerAvatarSnapshot>()
                .Where(x => x.DanceType != AvatarDanceType.None)
                .Select(x =>
                    (IComposer)
                        new DanceMessageComposer { ObjectId = x.ObjectId, DanceType = x.DanceType }
                ),
            .. avatarSnapshots
                .OfType<RoomBotAvatarSnapshot>()
                .Where(x => x.DanceType != AvatarDanceType.None)
                .Select(x =>
                    (IComposer)
                        new DanceMessageComposer { ObjectId = x.ObjectId, DanceType = x.DanceType }
                ),
        ];

        // Re-sync worn avatar effects the same way as dances: the effect is not in the Users wire payload,
        // so replay one AvatarEffectMessageComposer per occupant currently wearing an effect.
        IComposer[] effectComposers = avatarSnapshots
            .OfType<RoomPlayerAvatarSnapshot>()
            .Where(x => x.CurrentEffectId != 0)
            .Select(x =>
                (IComposer)
                    new AvatarEffectMessageComposer
                    {
                        UserId = x.ObjectId,
                        EffectId = x.CurrentEffectId,
                        DelayMilliseconds = 0,
                    }
            )
            .ToArray();

        // A hand item is no more part of the Users payload than a dance is, so whoever is holding
        // something has to be replayed for the person walking in.
        IComposer[] handItemComposers = avatarSnapshots
            .OfType<RoomPlayerAvatarSnapshot>()
            .Where(x => x.CarryItemId != 0)
            .Select(x =>
                (IComposer)
                    new CarryObjectMessageComposer
                    {
                        UserId = x.ObjectId.Value,
                        ItemType = x.CarryItemId,
                    }
            )
            .ToArray();

        await playerPresence
            .SendComposerAsync(
                new ObjectsMessageComposer
                {
                    OwnerNames = ownersSnapshot,
                    FloorItems = floorSnapshot,
                },
                new ItemsMessageComposer { OwnerNames = ownersSnapshot, WallItems = wallSnapshot },
                new UsersMessageComposer { Avatars = avatarSnapshots },
                new UserUpdateMessageComposer { Avatars = avatarSnapshots },
                new YouAreControllerMessageComposer
                {
                    RoomId = roomId,
                    ControllerLevel = controllerLevel,
                },
                new WiredPermissionsEventMessageComposer
                {
                    CanModify = hasRights,
                    CanRead = hasRights,
                }
            )
            .ConfigureAwait(false);

        if (isOwner)
        {
            await playerPresence
                .SendComposerAsync(new YouAreOwnerMessageComposer { RoomId = roomId })
                .ConfigureAwait(false);
        }

        if (danceComposers.Length > 0)
        {
            await playerPresence.SendComposerAsync(danceComposers).ConfigureAwait(false);
        }

        if (effectComposers.Length > 0)
        {
            await playerPresence.SendComposerAsync(effectComposers).ConfigureAwait(false);
        }

        if (handItemComposers.Length > 0)
        {
            await playerPresence.SendComposerAsync(handItemComposers).ConfigureAwait(false);
        }

        await playerPresence.SetActiveRoomAsync(roomId, ct).ConfigureAwait(false);
    }
}
