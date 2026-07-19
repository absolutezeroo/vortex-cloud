using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.FriendList;

namespace Vortex.PacketHandlers.FriendList;

public class FriendListUpdateMessageHandler : IMessageHandler<FriendListUpdateMessage>
{
    public async ValueTask HandleAsync(
        FriendListUpdateMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
