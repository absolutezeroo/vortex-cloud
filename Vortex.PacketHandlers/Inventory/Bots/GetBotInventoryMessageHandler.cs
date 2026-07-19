using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Inventory.Bots;
using Vortex.Primitives.Messages.Outgoing.Inventory.Bots;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Inventory.Bots;

public class GetBotInventoryMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetBotInventoryMessage>
{
    public async ValueTask HandleAsync(
        GetBotInventoryMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        await grainFactory
            .GetPlayerPresenceGrain(ctx.PlayerId)
            .SendComposerAsync(new BotInventoryEventMessageComposer())
            .ConfigureAwait(false);
    }
}
