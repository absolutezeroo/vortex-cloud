using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Quest;

namespace Vortex.PacketHandlers.Quest;

public class GetConcurrentUsersGoalProgressMessageHandler
    : IMessageHandler<GetConcurrentUsersGoalProgressMessage>
{
    public async ValueTask HandleAsync(
        GetConcurrentUsersGoalProgressMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
