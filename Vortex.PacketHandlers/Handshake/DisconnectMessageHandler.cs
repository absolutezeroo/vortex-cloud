using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Handshake;

namespace Vortex.PacketHandlers.Handshake;

public class DisconnectMessageHandler : IMessageHandler<DisconnectMessage>
{
    public async ValueTask HandleAsync(
        DisconnectMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ctx.CloseSessionAsync().ConfigureAwait(false);
    }
}
