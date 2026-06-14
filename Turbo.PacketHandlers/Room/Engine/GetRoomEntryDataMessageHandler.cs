using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Room.Engine;
using Turbo.Primitives.Messages.Outgoing.Room.Action;
using Turbo.Primitives.Messages.Outgoing.Room.Engine;
using Turbo.Primitives.Messages.Outgoing.Room.Permissions;
using Turbo.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Primitives.Rooms.Snapshots.Avatars;

namespace Turbo.PacketHandlers.Room.Engine;

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
            return;

        var playerPresence = _grainFactory.GetPlayerPresenceGrain(ctx.PlayerId);
        var pendingRoom = await playerPresence.GetPendingRoomAsync().ConfigureAwait(false);
        var roomId = pendingRoom.RoomId;

        if (roomId <= 0)
            return;

        var room = _grainFactory.GetRoomGrain(roomId);
        var ownersSnapshot = await room.GetAllOwnersAsync(ct).ConfigureAwait(false);
        var floorSnapshot = await room.GetAllFloorItemSnapshotsAsync(ct).ConfigureAwait(false);
        var wallSnapshot = await room.GetAllWallItemSnapshotsAsync(ct).ConfigureAwait(false);
        var avatarSnapshots = await room.GetAllAvatarSnapshotsAsync(ct).ConfigureAwait(false);

        var danceComposers = avatarSnapshots
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
                    ControllerLevel = RoomControllerType.Owner,
                },
                new WiredPermissionsEventMessageComposer { CanModify = true, CanRead = true },
                new YouAreOwnerMessageComposer { RoomId = roomId }
            )
            .ConfigureAwait(false);

        if (danceComposers.Length > 0)
            await playerPresence.SendComposerAsync(danceComposers).ConfigureAwait(false);

        await playerPresence.SetActiveRoomAsync(roomId, ct).ConfigureAwait(false);
    }
}
