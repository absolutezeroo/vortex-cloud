using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Collectibles;

namespace Vortex.PacketHandlers.Collectibles;

/// <summary>
/// Taking the reward at the end of a finished collection.
/// <para>
/// Unanswered for now, and deliberately paired with the collections themselves reporting every
/// claim as not-claimable: nothing hands the furniture over yet, so the client draws the prize out
/// of reach rather than lighting a button that would do nothing. Handing it over is the next slice
/// — it needs the grant to be idempotent, or a double click is a double prize.
/// </para>
/// </summary>
public class NftCollectiblesClaimRewardItemMessageHandler
    : IMessageHandler<NftCollectiblesClaimRewardItemMessage>
{
    public async ValueTask HandleAsync(
        NftCollectiblesClaimRewardItemMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
