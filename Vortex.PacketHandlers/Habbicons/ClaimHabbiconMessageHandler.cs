using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Habbicons;

namespace Vortex.PacketHandlers.Habbicons;

/// <summary>
/// Claim a completed set's bonus. The client marks a bonus claimable on its own as soon as its
/// album fills up, so the grain recomputes completion from stored ownership before granting.
/// </summary>
public class ClaimHabbiconMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<ClaimHabbiconMessage>
{
    public async ValueTask HandleAsync(
        ClaimHabbiconMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.HabbiconId <= 0)
        {
            return;
        }

        await grainFactory
            .GetPlayerHabbiconGrain(ctx.PlayerId)
            .ClaimCollectionRewardAsync(message.HabbiconId, ct)
            .ConfigureAwait(false);
    }
}
