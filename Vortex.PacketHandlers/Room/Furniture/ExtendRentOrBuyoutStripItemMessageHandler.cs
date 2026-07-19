using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Furniture;

namespace Vortex.PacketHandlers.Room.Furniture;

public class ExtendRentOrBuyoutStripItemMessageHandler
    : IMessageHandler<ExtendRentOrBuyoutStripItemMessage>
{
    public async ValueTask HandleAsync(
        ExtendRentOrBuyoutStripItemMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
