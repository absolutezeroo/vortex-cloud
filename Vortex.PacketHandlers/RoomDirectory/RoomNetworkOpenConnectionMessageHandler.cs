using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Roomdirectory;

namespace Vortex.PacketHandlers.RoomDirectory;

public class RoomNetworkOpenConnectionMessageHandler
    : IMessageHandler<RoomNetworkOpenConnectionMessage>
{
    public async ValueTask HandleAsync(
        RoomNetworkOpenConnectionMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
