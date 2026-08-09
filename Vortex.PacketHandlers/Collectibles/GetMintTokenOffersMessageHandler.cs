using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Collectibles;

namespace Vortex.PacketHandlers.Collectibles;

/// <summary>
/// The shop selling mint tokens.
/// <para>
/// Deliberately unanswered: minting is a blockchain errand and this hotel has no chain, no wallet
/// and no token contract. The server says so once, through
/// <c>GetCollectibleMintingEnabled</c>, and the client puts the whole minting half of the
/// interface away — so anything still arriving here is a stray click rather than a question owed
/// an answer. Inventing a reply would draw an interface backed by nothing.
/// </para>
/// </summary>
public class GetMintTokenOffersMessageHandler : IMessageHandler<GetMintTokenOffersMessage>
{
    public async ValueTask HandleAsync(
        GetMintTokenOffersMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
