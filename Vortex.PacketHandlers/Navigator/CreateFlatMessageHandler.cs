using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Rooms;
using Vortex.Protocol.Messages.Incoming.Navigator;
using Vortex.Protocol.Messages.Outgoing.Navigator;
using Vortex.Protocol.Messages.Outgoing.Room.Session;

namespace Vortex.PacketHandlers.Navigator;

public class CreateFlatMessageHandler(IRoomService roomService) : IMessageHandler<CreateFlatMessage>
{
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        CreateFlatMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        (RoomId roomId, string name) = await _roomService
            .CreateRoomAsync(
                message.FlatName,
                message.FlatDescription,
                message.FlatModelName,
                message.CategoryID,
                message.MaxPlayers,
                message.TradeSetting,
                ctx.PlayerId,
                ct
            )
            .ConfigureAwait(false);

        // FlatCreated alone does not move the player: the client only listens for it in its guild
        // window, to add the room to a dropdown. Walking into the room you just made is what
        // RoomForward is for -- the navigator answers it with forwardToRoom.
        await ctx.SendComposerAsync(
                new FlatCreatedMessageComposer { RoomId = roomId, Name = name },
                ct
            )
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(new RoomForwardMessageComposer { RoomId = roomId }, ct)
            .ConfigureAwait(false);
    }
}
