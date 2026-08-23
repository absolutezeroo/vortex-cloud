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
/// Converting a piece of furniture into a Relic.
/// </summary>
/// <remarks>
/// <para>
/// The conversion belongs to the player's mint grain, which is single-threaded — the confirm dialog
/// stays clickable until the answer comes back, so two quick clicks arrive as two messages and only
/// one of them may consume anything.
/// </para>
/// <para>
/// The stamp balance is pushed before the result. The tab does not re-ask for it after a
/// conversion — it redraws from the number it already has — so without this the price of what was
/// just spent stays on screen.
/// </para>
/// </remarks>
public class MintItemMessageHandler(
    IGrainFactory grainFactory,
    ILogger<MintItemMessageHandler> logger
) : IMessageHandler<MintItemMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly ILogger<MintItemMessageHandler> _logger = logger;

    public async ValueTask HandleAsync(
        MintItemMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            _logger.LogWarning(
                "A mint of item {ItemId} arrived on a session with no player.",
                message.ItemId
            );

            return;
        }

        IPlayerMintGrain mint = _grainFactory.GetPlayerMintGrain(new PlayerId(ctx.PlayerId));

        MintOutcome outcome = await mint.MintAsync(message.ItemId, ct).ConfigureAwait(false);

        // Named in the log whatever it was: the client cannot tell one refusal from another, so
        // without this a refused conversion leaves no trace at all.
        _logger.LogInformation(
            "Mint of item {ItemId} by player {PlayerId}: {Outcome}.",
            message.ItemId,
            ctx.PlayerId,
            outcome
        );

        int balance = await mint.GetTokenBalanceAsync(ct).ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new CollectibleMintTokenCountMessageComposer { Count = balance },
                ct
            )
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new CollectibleMintableItemResultMessageComposer
                {
                    // Success is 1 here and 0 nearly everywhere else in this domain. Sending the
                    // wrong one is silent: the player is told a Relic was minted that does not exist.
                    Status =
                        outcome == MintOutcome.Minted
                            ? CollectibleMintableItemResultMessageComposer.Success
                            : CollectibleMintableItemResultMessageComposer.Failed,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
