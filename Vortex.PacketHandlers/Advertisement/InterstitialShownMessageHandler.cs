using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Advertisement;

namespace Vortex.PacketHandlers.Advertisement;

public class InterstitialShownMessageHandler : IMessageHandler<InterstitialShownMessage>
{
    public async ValueTask HandleAsync(
        InterstitialShownMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
