using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Collectibles;

namespace Vortex.PacketHandlers.Collectibles;

public class GetCollectorScoreMessageHandler : IMessageHandler<GetCollectorScoreMessage>
{
    public async ValueTask HandleAsync(
        GetCollectorScoreMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
