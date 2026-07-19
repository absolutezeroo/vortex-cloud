using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Camera;

namespace Vortex.PacketHandlers.Camera;

public class PurchasePhotoMessageHandler : IMessageHandler<PurchasePhotoMessage>
{
    public async ValueTask HandleAsync(
        PurchasePhotoMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
