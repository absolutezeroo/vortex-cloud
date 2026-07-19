using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Nft;

namespace Vortex.PacketHandlers.Nft;

public class GetUserNftWardrobeMessageHandler : IMessageHandler<GetUserNftWardrobeMessage>
{
    public async ValueTask HandleAsync(
        GetUserNftWardrobeMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
