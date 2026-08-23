using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Collectibles;
using Vortex.Protocol.Messages.Incoming.Collectibles;
using Vortex.Protocol.Messages.Outgoing.Collectibles;

namespace Vortex.PacketHandlers.Collectibles;

/// <summary>
/// What a transfer would cost. Nothing, because no transfer can happen.
/// </summary>
/// <remarks>
/// Answering matters more than the answer. This handler used to return without sending anything,
/// and the collectibles interface waits on every one of these -- so silence left it loading rather
/// than showing that the feature is off.
/// </remarks>
public class GetNftTransferFeeMessageHandler : IMessageHandler<GetNftTransferFeeMessage>
{
    public async ValueTask HandleAsync(
        GetNftTransferFeeMessage message,
        MessageContext ctx,
        CancellationToken ct
    ) =>
        await ctx.SendComposerAsync(new NftTransferFeeMessageComposer { Fee = 0 }, ct)
            .ConfigureAwait(false);
}
