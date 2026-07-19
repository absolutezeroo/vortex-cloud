using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Collectibles;

namespace Vortex.PacketHandlers.Collectibles;

public class NftCollectiblesClaimBonusItemMessageHandler
    : IMessageHandler<NftCollectiblesClaimBonusItemMessage>
{
    public async ValueTask HandleAsync(
        NftCollectiblesClaimBonusItemMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
