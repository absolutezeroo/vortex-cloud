using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Inventory.Grains;
using Vortex.Primitives.Messages.Incoming.Inventory.Pets;
using Vortex.Primitives.Messages.Outgoing.Inventory.Pets;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Pets.Snapshots;

namespace Vortex.PacketHandlers.Inventory.Pets;

public class GetPetInventoryMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetPetInventoryMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetPetInventoryMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        IInventoryGrain inventoryGrain = _grainFactory.GetInventoryGrain(ctx.PlayerId);
        ImmutableArray<PetSnapshot> pets = await inventoryGrain
            .GetAllPetSnapshotsAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(new PetInventoryEventMessageComposer { Pets = pets }, ct)
            .ConfigureAwait(false);
    }
}
