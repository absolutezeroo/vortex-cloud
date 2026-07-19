using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Nft;

namespace Vortex.PacketHandlers.Nft;

public class GetSelectedNftWardrobeOutfitMessageHandler
    : IMessageHandler<GetSelectedNftWardrobeOutfitMessage>
{
    public async ValueTask HandleAsync(
        GetSelectedNftWardrobeOutfitMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
