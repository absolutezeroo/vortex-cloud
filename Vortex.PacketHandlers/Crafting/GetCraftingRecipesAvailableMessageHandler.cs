using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Crafting;

namespace Vortex.PacketHandlers.Crafting;

public class GetCraftingRecipesAvailableMessageHandler
    : IMessageHandler<GetCraftingRecipesAvailableMessage>
{
    public async ValueTask HandleAsync(
        GetCraftingRecipesAvailableMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
