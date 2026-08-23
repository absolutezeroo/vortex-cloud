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
/// The Relics waiting for a player in the Collectors Guild's Rewards tab.
/// </summary>
/// <remarks>
/// This used to answer an empty list on the grounds that a claim is a token minted against a chain
/// the hotel does not have. Like the shop, that was a description of what had not been built rather
/// than of what was impossible: a claim here is "this player may take this piece of furniture", and
/// an admin fills them in. An empty list now means nothing is waiting.
/// </remarks>
public class GetNftClaimsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetNftClaimsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetNftClaimsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        // The tab asks once per wallet and clears its waiting flag on the answer, so every request
        // is answered even when there is nothing to send.
        ImmutableArray<NftClaimSnapshot> claims = await _grainFactory
            .GetPlayerNftClaimsGrain(new PlayerId(ctx.PlayerId))
            .GetClaimsAsync(message.Wallet, ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(new NftClaimsMessageComposer { Claims = claims }, ct)
            .ConfigureAwait(false);
    }
}
