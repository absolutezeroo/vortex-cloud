using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Advertisement;

namespace Vortex.PacketHandlers.Advertisement;

public class GetInterstitialMessageHandler : IMessageHandler<GetInterstitialMessage>
{
    public async ValueTask HandleAsync(
        GetInterstitialMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
