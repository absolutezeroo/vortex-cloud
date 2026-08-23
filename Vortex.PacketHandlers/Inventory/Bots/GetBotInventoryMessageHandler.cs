using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Bots;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Inventory.Bots;
using Vortex.Protocol.Messages.Outgoing.Inventory.Bots;

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

        ImmutableArray<BotSnapshot> bots = await grainFactory
            .GetInventoryGrain(ctx.PlayerId)
            .GetAllBotSnapshotsAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(new BotInventoryEventMessageComposer { Bots = bots }, ct)
            .ConfigureAwait(false);
    }
}
