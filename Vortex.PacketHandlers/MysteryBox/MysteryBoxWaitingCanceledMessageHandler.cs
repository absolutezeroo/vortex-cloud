using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.Mysterybox;
using Vortex.Primitives.Rooms;

namespace Vortex.PacketHandlers.MysteryBox;

public class MysteryBoxWaitingCanceledMessageHandler(IRoomService roomService)
    : IMessageHandler<MysteryBoxWaitingCanceledMessage>
{
    private readonly IRoomService _roomService = roomService;

    public async ValueTask HandleAsync(
        MysteryBoxWaitingCanceledMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await _roomService
            .CancelMysteryBoxWaitAsync(ctx.AsActionContext(), message.BoxOwnerId, ct)
            .ConfigureAwait(false);
    }
}
