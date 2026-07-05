using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Inventory.Bots;
using Turbo.Primitives.Messages.Outgoing.Inventory.Bots;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Inventory.Bots;

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
