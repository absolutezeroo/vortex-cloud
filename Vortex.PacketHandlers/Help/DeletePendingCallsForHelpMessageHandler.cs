using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Help;

namespace Vortex.PacketHandlers.Help;

public class DeletePendingCallsForHelpMessageHandler
    : IMessageHandler<DeletePendingCallsForHelpMessage>
{
    public async ValueTask HandleAsync(
        DeletePendingCallsForHelpMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
