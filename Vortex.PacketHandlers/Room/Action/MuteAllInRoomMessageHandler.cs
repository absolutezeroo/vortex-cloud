using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Action;

namespace Vortex.PacketHandlers.Room.Action;

public class MuteAllInRoomMessageHandler : IMessageHandler<MuteAllInRoomMessage>
{
    public async ValueTask HandleAsync(
        MuteAllInRoomMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
