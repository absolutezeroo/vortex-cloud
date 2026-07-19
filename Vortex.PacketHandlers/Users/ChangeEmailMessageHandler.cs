using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Users;

namespace Vortex.PacketHandlers.Users;

public class ChangeEmailMessageHandler : IMessageHandler<ChangeEmailMessage>
{
    public async ValueTask HandleAsync(
        ChangeEmailMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
