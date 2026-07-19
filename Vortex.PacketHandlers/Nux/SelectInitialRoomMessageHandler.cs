using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Nux;

namespace Vortex.PacketHandlers.Nux;

public class SelectInitialRoomMessageHandler : IMessageHandler<SelectInitialRoomMessage>
{
    public async ValueTask HandleAsync(
        SelectInitialRoomMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
