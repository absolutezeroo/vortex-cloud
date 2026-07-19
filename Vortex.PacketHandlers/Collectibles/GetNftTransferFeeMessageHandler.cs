using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Collectibles;

namespace Vortex.PacketHandlers.Collectibles;

public class GetNftTransferFeeMessageHandler : IMessageHandler<GetNftTransferFeeMessage>
{
    public async ValueTask HandleAsync(
        GetNftTransferFeeMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
