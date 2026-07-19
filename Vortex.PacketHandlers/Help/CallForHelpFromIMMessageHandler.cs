using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Help;

namespace Vortex.PacketHandlers.Help;

public class CallForHelpFromIMMessageHandler : IMessageHandler<CallForHelpFromIMMessage>
{
    public async ValueTask HandleAsync(
        CallForHelpFromIMMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
