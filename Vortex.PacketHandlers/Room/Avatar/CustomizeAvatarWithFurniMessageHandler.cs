using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Avatar;

namespace Vortex.PacketHandlers.Room.Avatar;

public class CustomizeAvatarWithFurniMessageHandler
    : IMessageHandler<CustomizeAvatarWithFurniMessage>
{
    public async ValueTask HandleAsync(
        CustomizeAvatarWithFurniMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
