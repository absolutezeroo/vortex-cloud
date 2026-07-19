using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Crafting;

namespace Vortex.PacketHandlers.Crafting;

public class GetCraftableProductsMessageHandler : IMessageHandler<GetCraftableProductsMessage>
{
    public async ValueTask HandleAsync(
        GetCraftableProductsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
