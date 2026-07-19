using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Avatar;

namespace Vortex.PacketHandlers.Avatar;

public class ChangeUserNameInRoomMessageHandler : IMessageHandler<ChangeUserNameInRoomMessage>
{
    public async ValueTask HandleAsync(
        ChangeUserNameInRoomMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
