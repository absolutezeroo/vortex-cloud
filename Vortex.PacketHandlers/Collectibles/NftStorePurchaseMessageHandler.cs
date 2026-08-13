using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Collectibles.Grains;
using Vortex.Primitives.Messages.Incoming.Collectibles;
using Vortex.Primitives.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;

namespace Vortex.PacketHandlers.Collectibles;

/// <summary>
/// Buying from the Collectors Guild shop.
/// </summary>
/// <remarks>
/// The sale itself belongs to the shop grain: it is single-threaded, so the last copy of a limited
/// edition cannot be sold twice however many players click at once. This only asks and reports.
/// </remarks>
public class NftStorePurchaseMessageHandler(
    IGrainFactory grainFactory,
    ILogger<NftStorePurchaseMessageHandler> logger
) : IMessageHandler<NftStorePurchaseMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly ILogger<NftStorePurchaseMessageHandler> _logger = logger;

    public async ValueTask HandleAsync(
        NftStorePurchaseMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            // Logged rather than dropped: a purchase from a session with no player attached is a
            // real fault, and returning quietly here is indistinguishable from the packet never
            // arriving at all.
            _logger.LogWarning(
                "Collectibles shop purchase of {ProductCode} arrived on a session with no player.",
                message.ProductCode
            );

            return;
        }

        NftStorePurchaseOutcome outcome = await _grainFactory
            .GetNftStoreGrain()
            .PurchaseAsync(new PlayerId(ctx.PlayerId), message.ProductCode, ct)
            .ConfigureAwait(false);

        // Every outcome is named here, not just the two the grain already logs. The client cannot
        // tell them apart — it alerts on anything that is not success — so without this a refusal
        // leaves no trace anywhere and looks exactly like a packet that never arrived.
        _logger.LogInformation(
            "Collectibles shop purchase of {ProductCode} by player {PlayerId}: {Outcome}.",
            message.ProductCode,
            ctx.PlayerId,
            outcome
        );

        await ctx.SendComposerAsync(
                new NftStorePurchaseMessageComposer
                {
                    // Every refusal is the same alert to the client; the reason lives in the log.
                    Result =
                        outcome == NftStorePurchaseOutcome.Sold
                            ? NftStorePurchaseMessageComposer.Success
                            : NftStorePurchaseMessageComposer.Error,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
