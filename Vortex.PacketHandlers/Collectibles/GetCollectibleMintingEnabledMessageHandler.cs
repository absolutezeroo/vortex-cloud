using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.Collectibles;
using Vortex.Protocol.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Collectibles;

/// <summary>
/// Whether this hotel mints.
/// </summary>
/// <remarks>
/// It answered no for a long time, on the grounds that minting is a blockchain errand. It is not:
/// the client sends the <em>inventory id</em> of a piece of furniture the player already owns, and
/// what comes back is a Relic — an item converted, which is exactly what the badge text "Converted
/// N items to Relics" describes. Stamps are bought with silver. No chain appears anywhere in it.
/// <para>
/// The answer is now an admin switch, because an emulator that has configured nothing to convert is
/// still better off hiding the tab's minting half than showing buttons over an empty list.
/// </para>
/// </remarks>
public class GetCollectibleMintingEnabledMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetCollectibleMintingEnabledMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetCollectibleMintingEnabledMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        bool enabled = await _grainFactory
            .GetNftMintingGrain()
            .IsMintingEnabledAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new CollectibleMintingEnabledMessageComposer { Enabled = enabled },
                ct
            )
            .ConfigureAwait(false);
    }
}
