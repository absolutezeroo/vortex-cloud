using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Action;
using Vortex.Primitives.Messages.Incoming.Room.Engine;
using Vortex.Primitives.Messages.Outgoing.Room.Action;
using Vortex.Primitives.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Messages.Outgoing.Room.Permissions;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
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

        IRoomGrain room = _grainFactory.GetRoomGrain(roomId);
        ActionContext actionCtx = ActionContext.CreateForPlayer(ctx.PlayerId, roomId);
        RoomControllerType controllerLevel = await room.GetControllerLevelAsync(actionCtx, ct)
            .ConfigureAwait(false);
        bool isOwner = controllerLevel == RoomControllerType.Owner;
        bool hasRights = controllerLevel >= RoomControllerType.Rights;

        ImmutableDictionary<PlayerId, string> ownersSnapshot = await room.GetAllOwnersAsync(ct)
            .ConfigureAwait(false);
        ImmutableArray<RoomFloorItemSnapshot> floorSnapshot =
            await room.GetAllFloorItemSnapshotsAsync(ct).ConfigureAwait(false);
        ImmutableArray<RoomWallItemSnapshot> wallSnapshot = await room.GetAllWallItemSnapshotsAsync(
                ct
            )
            .ConfigureAwait(false);
        ImmutableArray<RoomAvatarSnapshot> avatarSnapshots = await room.GetAllAvatarSnapshotsAsync(
                ct
            )
            .ConfigureAwait(false);

        IComposer[] danceComposers = avatarSnapshots
            .OfType<RoomPlayerAvatarSnapshot>()
            .Where(x => x.DanceType != AvatarDanceType.None)
            .Select(x =>
                (IComposer)
                    new DanceMessageComposer { ObjectId = x.ObjectId, DanceType = x.DanceType }
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

        await playerPresence.SetActiveRoomAsync(roomId, ct).ConfigureAwait(false);
    }
}
