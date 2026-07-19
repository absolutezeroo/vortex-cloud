using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Camera;

namespace Vortex.PacketHandlers.Camera;

public class PublishPhotoMessageHandler : IMessageHandler<PublishPhotoMessage>
{
    public async ValueTask HandleAsync(
        PublishPhotoMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
