using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Collectibles;
using Vortex.Primitives.Messages.Incoming.Collectibles;
using Vortex.Primitives.Messages.Outgoing.Collectibles;

namespace Vortex.PacketHandlers.Collectibles;

/// <summary>
/// The claims waiting on a wallet. There are none and there cannot be: a claim is a token minted
/// against a chain this hotel does not have.
/// </summary>
/// <remarks>
/// The empty list is the point. The request used to have no parser at all, so it was dropped before
/// reaching any handler and the claims tab waited on an answer that was never coming.
/// </remarks>
public class GetNftClaimsMessageHandler : IMessageHandler<GetNftClaimsMessage>
{
    public async ValueTask HandleAsync(
        GetNftClaimsMessage message,
        MessageContext ctx,
        CancellationToken ct
    ) =>
        await ctx.SendComposerAsync(
                new NftClaimsMessageComposer { Claims = ImmutableArray<NftClaimSnapshot>.Empty },
                ct
            )
            .ConfigureAwait(false);
}
