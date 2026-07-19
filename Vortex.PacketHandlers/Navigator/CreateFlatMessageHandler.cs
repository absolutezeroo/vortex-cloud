using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Navigator;
using Vortex.Primitives.Messages.Outgoing.Navigator;
using Vortex.Primitives.Rooms;

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

        await ctx.SendComposerAsync(
                new FlatCreatedMessageComposer { RoomId = roomId, Name = name },
                ct
            )
            .ConfigureAwait(false);
    }
}
