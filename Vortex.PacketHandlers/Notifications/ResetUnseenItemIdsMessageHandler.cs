using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Notifications;

namespace Vortex.PacketHandlers.Notifications;

public class ResetUnseenItemIdsMessageHandler : IMessageHandler<ResetUnseenItemIdsMessage>
{
    public async ValueTask HandleAsync(
        ResetUnseenItemIdsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
