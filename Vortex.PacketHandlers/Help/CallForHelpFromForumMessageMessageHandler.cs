using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Help;

namespace Vortex.PacketHandlers.Help;

public class CallForHelpFromForumMessageMessageHandler
    : IMessageHandler<CallForHelpFromForumMessageMessage>
{
    public async ValueTask HandleAsync(
        CallForHelpFromForumMessageMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
