using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Collectibles;
using Vortex.Primitives.Messages.Outgoing.Collectibles;

namespace Vortex.PacketHandlers.Collectibles;

/// <summary>
/// Claiming an item into a wallet. Refused -- there is no wallet and no chain behind it.
/// </summary>
/// <remarks>
/// The refusal is explicit rather than implied. This used to send nothing, and the interface waited;
/// now it carries success=false, which the client renders as a failed claim instead of a pending one.
/// The two strings come back empty because the request does not name them and there is nothing to
/// echo -- they still have to be written or the boolean lands in the wrong place.
/// </remarks>
public class NftCollectiblesClaimBonusItemMessageHandler
    : IMessageHandler<NftCollectiblesClaimBonusItemMessage>
{
    public async ValueTask HandleAsync(
        NftCollectiblesClaimBonusItemMessage message,
        MessageContext ctx,
        CancellationToken ct
    ) =>
        await ctx.SendComposerAsync(
                new NftBonusItemClaimResultMessageComposer
                {
                    CollectionId = string.Empty,
                    WalletAddress = string.Empty,
                    Success = false,
                },
                ct
            )
            .ConfigureAwait(false);
}
