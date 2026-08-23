using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Collectibles.Grains;
using Vortex.Protocol.Messages.Incoming.Collectibles;
using Vortex.Protocol.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;

namespace Vortex.PacketHandlers.Collectibles;

/// <summary>
/// Buying stamps with silver.
/// </summary>
/// <remarks>
/// There is no result message for this: the tab shows whatever balance comes back and nothing else,
/// so a refusal is reported by sending the unchanged balance. That is why the balance is sent on
/// every path, including the ones that bought nothing — silence would leave the old number on
/// screen and the player would think it worked.
/// </remarks>
public class PurchaseMintTokenMessageHandler(
    IGrainFactory grainFactory,
    ILogger<PurchaseMintTokenMessageHandler> logger
) : IMessageHandler<PurchaseMintTokenMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly ILogger<PurchaseMintTokenMessageHandler> _logger = logger;

    public async ValueTask HandleAsync(
        PurchaseMintTokenMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            _logger.LogWarning(
                "A stamp purchase for offer {OfferId} arrived on a session with no player.",
                message.OfferId
            );

            return;
        }

        MintTokenPurchaseResult result = await _grainFactory
            .GetPlayerMintGrain(new PlayerId(ctx.PlayerId))
            .PurchaseTokensAsync(message.OfferId, ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new CollectibleMintTokenCountMessageComposer { Count = result.Balance },
                ct
            )
            .ConfigureAwait(false);
    }
}
