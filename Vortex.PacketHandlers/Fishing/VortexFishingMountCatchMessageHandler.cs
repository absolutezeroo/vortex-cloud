using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Fishing;

namespace Vortex.PacketHandlers.Fishing;

/// <summary>
/// Mints a trophy from one of the player's recorded catches. Vortex-specific: no AS3 or Habbo
/// equivalent.
/// </summary>
/// <remarks>
/// The record id is the client's only say in this. Whether it is theirs, what species and weight the
/// trophy is inscribed with, and which furniture it becomes are all read from the stored row inside
/// the grain — so a guessed id buys nothing.
/// </remarks>
public class VortexFishingMountCatchMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<VortexFishingMountCatchMessage>
{
    public async ValueTask HandleAsync(
        VortexFishingMountCatchMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        await grainFactory
            .GetFishingPlayerGrain(ctx.PlayerId)
            .MountRecordAsync(message.RecordId, ct)
            .ConfigureAwait(false);
    }
}
