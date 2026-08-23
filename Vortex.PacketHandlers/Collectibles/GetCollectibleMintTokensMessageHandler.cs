using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.Collectibles;
using Vortex.Protocol.Messages.Outgoing.Collectibles;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;

namespace Vortex.PacketHandlers.Collectibles;

/// <summary>
/// How many stamps the player holds.
/// </summary>
/// <remarks>
/// Asked per wallet, and every player here has exactly one — so the address that arrives is not used
/// to pick a balance. The session's player is.
/// </remarks>
public class GetCollectibleMintTokensMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetCollectibleMintTokensMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetCollectibleMintTokensMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        int balance = await _grainFactory
            .GetPlayerMintGrain(new PlayerId(ctx.PlayerId))
            .GetTokenBalanceAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new CollectibleMintTokenCountMessageComposer { Count = balance },
                ct
            )
            .ConfigureAwait(false);
    }
}
