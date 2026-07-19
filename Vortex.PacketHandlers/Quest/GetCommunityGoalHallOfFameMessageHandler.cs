using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Quest;

namespace Vortex.PacketHandlers.Quest;

public class GetCommunityGoalHallOfFameMessageHandler
    : IMessageHandler<GetCommunityGoalHallOfFameMessage>
{
    public async ValueTask HandleAsync(
        GetCommunityGoalHallOfFameMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
