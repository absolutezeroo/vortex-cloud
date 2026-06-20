using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Navigator;
using Turbo.Primitives.Messages.Outgoing.Navigator;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Snapshots.Room;
using Turbo.Primitives.Rooms;
using Turbo.Primitives.Rooms.Grains;

namespace Turbo.PacketHandlers.Navigator;

public class GetGuestRoomMessageHandler(IRoomService roomService, IGrainFactory grainFactory)
    : IMessageHandler<GetGuestRoomMessage>
{
    private readonly IRoomService _roomService = roomService;
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetGuestRoomMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        IRoomGrain roomGrain = _grainFactory.GetRoomGrain(message.RoomId);
        RoomSnapshot snapshot = await roomGrain.GetSnapshotAsync().ConfigureAwait(false);

        bool staffPick = false;
        bool groupMember = false;
        bool allInRoomMuted = false;
        bool canMute = false;

        await ctx.SendComposerAsync(
                new GetGuestRoomResultMessageComposer
                {
                    EnterRoom = message.EnterRoom,
                    RoomInfo = snapshot,
                    RoomForward = message.RoomForward,
                    StaffPick = staffPick,
                    IsGroupMember = groupMember,
                    AllInRoomMuted = allInRoomMuted,
                    CanMute = canMute,
                },
                ct
            )
            .ConfigureAwait(false);

        await _roomService
            .OpenRoomForPlayerIdAsync(ctx.AsActionContext(), ctx.PlayerId, message.RoomId, ct)
            .ConfigureAwait(false);
    }
}
