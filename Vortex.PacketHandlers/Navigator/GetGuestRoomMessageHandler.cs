using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Navigator;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Navigator;

public class GetGuestRoomMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetGuestRoomMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public ValueTask HandleAsync(
        GetGuestRoomMessage message,
        MessageContext ctx,
        CancellationToken ct
    ) =>
        RoomForwardHelper.SendGuestRoomResultAsync(
            _grainFactory,
            ctx,
            message.RoomId,
            message.EnterRoom,
            message.RoomForward,
            ct
        );
}
