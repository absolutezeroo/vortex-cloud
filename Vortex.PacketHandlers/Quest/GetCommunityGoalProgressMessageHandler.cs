using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Quest;

namespace Vortex.PacketHandlers.Quest;

public class GetCommunityGoalProgressMessageHandler
    : IMessageHandler<GetCommunityGoalProgressMessage>
{
    public async ValueTask HandleAsync(
        GetCommunityGoalProgressMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
