using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Catalog;

namespace Vortex.PacketHandlers.Catalog;

public class GetNextTargetedOfferMessageHandler : IMessageHandler<GetNextTargetedOfferMessage>
{
    public async ValueTask HandleAsync(
        GetNextTargetedOfferMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
