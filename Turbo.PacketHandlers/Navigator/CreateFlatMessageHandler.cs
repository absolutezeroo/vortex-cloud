using System.Threading;
using System.Threading.Tasks;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Navigator;
using Turbo.Primitives.Messages.Outgoing.Navigator;
using Turbo.Primitives.Rooms;

namespace Turbo.PacketHandlers.Navigator;

public class CreateFlatMessageHandler(IRoomService roomService) : IMessageHandler<CreateFlatMessage>
{
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        CreateFlatMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        var (roomId, name) = await _roomService
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
