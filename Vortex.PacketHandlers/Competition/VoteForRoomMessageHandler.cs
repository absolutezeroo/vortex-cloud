using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.Competition;

namespace Vortex.PacketHandlers.Competition;

public class VoteForRoomMessageHandler : IMessageHandler<VoteForRoomMessage>
{
    public async ValueTask HandleAsync(
        VoteForRoomMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
