using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Competition;

namespace Vortex.PacketHandlers.Competition;

public class RoomCompetitionInitMessageHandler : IMessageHandler<RoomCompetitionInitMessage>
{
    public async ValueTask HandleAsync(
        RoomCompetitionInitMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
