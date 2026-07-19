using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Competition;

namespace Vortex.PacketHandlers.Competition;

public class ForwardToACompetitionRoomMessageHandler
    : IMessageHandler<ForwardToACompetitionRoomMessage>
{
    public async ValueTask HandleAsync(
        ForwardToACompetitionRoomMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
