using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Avatar;

namespace Vortex.PacketHandlers.Room.Avatar;

public class PassCarryItemToPetMessageHandler : IMessageHandler<PassCarryItemToPetMessage>
{
    public async ValueTask HandleAsync(
        PassCarryItemToPetMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
