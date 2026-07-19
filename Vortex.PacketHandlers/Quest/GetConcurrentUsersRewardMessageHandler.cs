using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Quest;

namespace Vortex.PacketHandlers.Quest;

public class GetConcurrentUsersRewardMessageHandler
    : IMessageHandler<GetConcurrentUsersRewardMessage>
{
    public async ValueTask HandleAsync(
        GetConcurrentUsersRewardMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
