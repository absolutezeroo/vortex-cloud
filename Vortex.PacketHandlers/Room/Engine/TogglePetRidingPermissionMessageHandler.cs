using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Engine;

namespace Vortex.PacketHandlers.Room.Engine;

public class TogglePetRidingPermissionMessageHandler
    : IMessageHandler<TogglePetRidingPermissionMessage>
{
    public async ValueTask HandleAsync(
        TogglePetRidingPermissionMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
