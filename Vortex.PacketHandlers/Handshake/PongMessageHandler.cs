using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Handshake;

namespace Vortex.PacketHandlers.Handshake;

public class PongMessageHandler : IMessageHandler<PongMessage>
{
    public async ValueTask HandleAsync(
        PongMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
