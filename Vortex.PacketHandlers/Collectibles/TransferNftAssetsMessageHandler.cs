using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.Collectibles;
using Vortex.Protocol.Messages.Outgoing.Collectibles;

namespace Vortex.PacketHandlers.Collectibles;

/// <summary>
/// The Transfer tab's "move my Relics to that address". Refused, and the tab itself is switched off.
/// </summary>
/// <remarks>
/// <para>
/// Not because of a chain — because of what the message is. It carries a single destination address
/// and nothing else, so it moves the player's <em>whole</em> collection at once with no way to
/// choose; and the addresses it offers as destinations are the same list the Collections tab uses to
/// pick which wallet to browse. There is no reading of that list that makes both tabs mean something
/// sensible on a hotel where every player has exactly one wallet.
/// </para>
/// <para>
/// Handing a Relic to somebody is done in the trade window instead — per item, with the
/// confirmation both sides already know. The tab is hidden by
/// <c>collectibles.transfer.enabled</c> in the client's external variables, so this only answers the
/// case of a client that still asks.
/// </para>
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
                    // Non-zero: this client reads success as code == 0, so a refusal that forgot to
                    // set one would report a transfer that never happened.
                    ResultCode = NftTransferAssetsResultMessageComposer.NotAvailable,
                },
                ct
            )
            .ConfigureAwait(false);
}
