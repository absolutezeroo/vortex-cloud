using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Collectibles;

namespace Vortex.PacketHandlers.Collectibles;

public class GetMintTokenOffersMessageHandler : IMessageHandler<GetMintTokenOffersMessage>
{
    public async ValueTask HandleAsync(
        GetMintTokenOffersMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
