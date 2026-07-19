using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Crafting;

namespace Vortex.PacketHandlers.Crafting;

public class CraftSecretMessageHandler : IMessageHandler<CraftSecretMessage>
{
    public async ValueTask HandleAsync(
        CraftSecretMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
