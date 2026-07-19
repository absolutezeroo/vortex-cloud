using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Users;

namespace Vortex.PacketHandlers.Users;

public class GetSelectedBadgesMessageHandler : IMessageHandler<GetSelectedBadgesMessage>
{
    public async ValueTask HandleAsync(
        GetSelectedBadgesMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
