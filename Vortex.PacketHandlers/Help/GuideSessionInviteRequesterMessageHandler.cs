using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Help;

namespace Vortex.PacketHandlers.Help;

public class GuideSessionInviteRequesterMessageHandler
    : IMessageHandler<GuideSessionInviteRequesterMessage>
{
    public async ValueTask HandleAsync(
        GuideSessionInviteRequesterMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
