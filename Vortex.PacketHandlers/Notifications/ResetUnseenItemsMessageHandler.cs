using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Notifications;

namespace Vortex.PacketHandlers.Notifications;

public class ResetUnseenItemsMessageHandler : IMessageHandler<ResetUnseenItemsMessage>
{
    public async ValueTask HandleAsync(
        ResetUnseenItemsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
