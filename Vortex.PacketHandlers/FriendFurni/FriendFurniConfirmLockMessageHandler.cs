using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Friendfurni;

namespace Vortex.PacketHandlers.FriendFurni;

public class FriendFurniConfirmLockMessageHandler : IMessageHandler<FriendFurniConfirmLockMessage>
{
    public async ValueTask HandleAsync(
        FriendFurniConfirmLockMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
