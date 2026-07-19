using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Furniture;

namespace Vortex.PacketHandlers.Room.Furniture;

public class GetRentOrBuyoutOfferMessageHandler : IMessageHandler<GetRentOrBuyoutOfferMessage>
{
    public async ValueTask HandleAsync(
        GetRentOrBuyoutOfferMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
