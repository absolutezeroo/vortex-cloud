using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Outgoing.Navigator;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Grains;

namespace Vortex.PacketHandlers.Navigator;

/// <summary>
///     Shared "send this room's guest-room card to the requesting player" orchestration, used both
///     by GetGuestRoomMessage itself and by the server-initiated forward flows (ConvertGlobalRoomId,
///     ForwardToARandomPromotedRoom, ForwardToSomeRoom) that resolve a target room without the
///     client naming one directly. EnterRoom=true is how the client is told to proceed with its own
///     connect handshake, same as clicking a room in a search result.
/// </summary>
internal static class RoomForwardHelper
{
    public static async ValueTask SendGuestRoomResultAsync(
        IGrainFactory grainFactory,
        MessageContext ctx,
        RoomId roomId,
        bool enterRoom,
        bool roomForward,
        CancellationToken ct
    )
    {
        IRoomGrain roomGrain = grainFactory.GetRoomGrain(roomId);
        RoomSnapshot snapshot = await roomGrain.GetSnapshotAsync().ConfigureAwait(false);

        // Drives the "you are a member" affordances on the room card; only worth a grain call when
        // the room actually belongs to a guild.
        bool isGroupMember =
            snapshot.GroupId is int groupId
            && groupId > 0
            && await grainFactory
                .GetGroupGrain(groupId)
                .GetMemberRankAsync(ctx.PlayerId, ct)
                .ConfigureAwait(false)
                is not null;

        await ctx.SendComposerAsync(
                new GetGuestRoomResultMessageComposer
                {
                    EnterRoom = enterRoom,
                    RoomInfo = snapshot,
                    RoomForward = roomForward,
                    StaffPick = snapshot.StaffPick,
                    IsGroupMember = isGroupMember,
                    AllInRoomMuted = false,
                    CanMute = false,
                    OpeningConnection = false,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
