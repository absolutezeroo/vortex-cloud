using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Catalog;

namespace Vortex.PacketHandlers.Catalog;

public class SetTargetedOfferStateMessageHandler : IMessageHandler<SetTargetedOfferStateMessage>
{
    public async ValueTask HandleAsync(
        SetTargetedOfferStateMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
