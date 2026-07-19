using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Furniture;

namespace Vortex.PacketHandlers.Room.Furniture;

public class ExtendRentOrBuyoutFurniMessageHandler : IMessageHandler<ExtendRentOrBuyoutFurniMessage>
{
    public async ValueTask HandleAsync(
        ExtendRentOrBuyoutFurniMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
