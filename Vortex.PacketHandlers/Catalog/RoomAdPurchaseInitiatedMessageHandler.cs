using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Catalog;

namespace Vortex.PacketHandlers.Catalog;

/// <summary>Legitimate no-op: matches the client's own sendRoomAdPurchaseInitiatedEvent(), which is
/// defined but never called anywhere in the WIN63 source -- purely a leftover analytics/tracking
/// hook the server was never meant to act on.</summary>
public class RoomAdPurchaseInitiatedMessageHandler : IMessageHandler<RoomAdPurchaseInitiatedMessage>
{
    public async ValueTask HandleAsync(
        RoomAdPurchaseInitiatedMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
