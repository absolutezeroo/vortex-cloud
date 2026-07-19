using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Camera;

namespace Vortex.PacketHandlers.Camera;

public class RenderRoomMessageHandler : IMessageHandler<RenderRoomMessage>
{
    public async ValueTask HandleAsync(
        RenderRoomMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
