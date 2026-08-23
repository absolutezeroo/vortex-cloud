using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Collectibles;
using Vortex.Protocol.Messages.Incoming.Collectibles;
using Vortex.Protocol.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;

namespace Vortex.PacketHandlers.Collectibles;

/// <summary>
/// The player's Relics, as the inventory's Collectibles tab lists them.
/// </summary>
/// <remarks>
/// Answering at all is what ends the tab's wait: it treats "list not initialised" as its loading
/// state, and only this message initialises it. It used to answer an empty list on the grounds that
/// an asset needs a chain — now the list holds whatever the player has converted.
/// </remarks>
public class GetNftAssetInventoryMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetNftAssetInventoryMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetNftAssetInventoryMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        ImmutableArray<CollectibleAssetSnapshot> assets =
            ctx.PlayerId > 0
                ? await _grainFactory
                    .GetPlayerMintGrain(new PlayerId(ctx.PlayerId))
                    .GetAssetsAsync(ct)
                    .ConfigureAwait(false)
                : [];

        await ctx.SendComposerAsync(
                new TradeNftAssetInventoryMessageComposer { Assets = assets },
                ct
            )
            .ConfigureAwait(false);
    }
}
