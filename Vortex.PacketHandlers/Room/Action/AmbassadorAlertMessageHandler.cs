using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Action;

namespace Vortex.PacketHandlers.Room.Action;

public class AmbassadorAlertMessageHandler : IMessageHandler<AmbassadorAlertMessage>
{
    public async ValueTask HandleAsync(
        AmbassadorAlertMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
