using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Outgoing.Navigator;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Snapshots.Room;
using Turbo.Primitives.Rooms;
using Turbo.Primitives.Rooms.Grains;

namespace Turbo.PacketHandlers.Navigator;

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

        await ctx.SendComposerAsync(
                new GetGuestRoomResultMessageComposer
                {
                    EnterRoom = enterRoom,
                    RoomInfo = snapshot,
                    RoomForward = roomForward,
                    StaffPick = snapshot.StaffPick,
                    IsGroupMember = false,
                    AllInRoomMuted = false,
                    CanMute = false,
                    OpeningConnection = false,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
