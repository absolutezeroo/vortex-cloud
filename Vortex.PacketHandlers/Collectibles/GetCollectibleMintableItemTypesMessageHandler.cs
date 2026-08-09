using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Collectibles;

namespace Vortex.PacketHandlers.Collectibles;

/// <summary>
/// Which furniture could be minted into a token.
/// <para>
/// Deliberately unanswered: minting is a blockchain errand and this hotel has no chain, no wallet
/// and no token contract. The server says so once, through
/// <c>GetCollectibleMintingEnabled</c>, and the client puts the whole minting half of the
/// interface away — so anything still arriving here is a stray click rather than a question owed
/// an answer. Inventing a reply would draw an interface backed by nothing.
/// </para>
/// </summary>
public class GetCollectibleMintableItemTypesMessageHandler
    : IMessageHandler<GetCollectibleMintableItemTypesMessage>
{
    public async ValueTask HandleAsync(
        GetCollectibleMintableItemTypesMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
