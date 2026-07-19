using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Users;

namespace Vortex.PacketHandlers.Users;

public class GetEmailStatusMessageHandler : IMessageHandler<GetEmailStatusMessage>
{
    public async ValueTask HandleAsync(
        GetEmailStatusMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
