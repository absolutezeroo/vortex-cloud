using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Moderator;

namespace Vortex.PacketHandlers.Moderator;

public class GetRoomVisitsMessageHandler : IMessageHandler<GetRoomVisitsMessage>
{
    public async ValueTask HandleAsync(
        GetRoomVisitsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
