using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Collectibles;
using Vortex.Primitives.Messages.Outgoing.Collectibles;

namespace Vortex.PacketHandlers.Collectibles;

/// <summary>
/// Moving assets to a wallet. Refused: there is no chain to move them on.
/// </summary>
/// <remarks>
/// This message had no header at all, so the transfer tab's confirm button reached nothing. The
/// result composer it answers with was already written and mapped -- only the request half was
/// missing, which is why the tab looked like it should work.
/// </remarks>
public class TransferNftAssetsMessageHandler : IMessageHandler<TransferNftAssetsMessage>
{
    public async ValueTask HandleAsync(
        TransferNftAssetsMessage message,
        MessageContext ctx,
        CancellationToken ct
    ) =>
        await ctx.SendComposerAsync(
                new NftTransferAssetsResultMessageComposer
                {
                    ResultCode = NftTransferAssetsResultMessageComposer.NotAvailable,
                },
                ct
            )
            .ConfigureAwait(false);
}
