using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Collectibles;
using Vortex.Primitives.Messages.Outgoing.Collectibles;

namespace Vortex.PacketHandlers.Collectibles;

/// <summary>
/// Claiming a token. Always fails, because completing one would mean moving an asset on a chain
/// this hotel has no wallet or contract for.
/// </summary>
/// <remarks>
/// Failing out loud rather than silently. The request had no parser, so the button did nothing at
/// all and the tab sat on it; a failure code at least ends the wait honestly.
/// </remarks>
public class ClaimNftClaimsMessageHandler : IMessageHandler<ClaimNftClaimsMessage>
{
    public async ValueTask HandleAsync(
        ClaimNftClaimsMessage message,
        MessageContext ctx,
        CancellationToken ct
    ) =>
        await ctx.SendComposerAsync(
                new NftClaimResultMessageComposer { Status = NftClaimStatus.Failed },
                ct
            )
            .ConfigureAwait(false);
}
