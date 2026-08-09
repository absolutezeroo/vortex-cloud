using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Collectibles;

namespace Vortex.PacketHandlers.Collectibles;

/// <summary>
/// How many mint tokens the player holds. Tokens buy mints; there is nothing to mint.
/// <para>
/// Deliberately unanswered: minting is a blockchain errand and this hotel has no chain, no wallet
/// and no token contract. The server says so once, through
/// <c>GetCollectibleMintingEnabled</c>, and the client puts the whole minting half of the
/// interface away — so anything still arriving here is a stray click rather than a question owed
/// an answer. Inventing a reply would draw an interface backed by nothing.
/// </para>
/// </summary>
public class GetCollectibleMintTokensMessageHandler
    : IMessageHandler<GetCollectibleMintTokensMessage>
{
    public async ValueTask HandleAsync(
        GetCollectibleMintTokensMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
