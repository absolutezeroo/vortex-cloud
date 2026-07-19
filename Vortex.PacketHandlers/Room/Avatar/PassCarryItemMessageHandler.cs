using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Avatar;

namespace Vortex.PacketHandlers.Room.Avatar;

public class PassCarryItemMessageHandler : IMessageHandler<PassCarryItemMessage>
{
    public async ValueTask HandleAsync(
        PassCarryItemMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
