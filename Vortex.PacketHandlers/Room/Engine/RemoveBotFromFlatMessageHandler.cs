using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Room.Engine;

namespace Vortex.PacketHandlers.Room.Engine;

public class RemoveBotFromFlatMessageHandler : IMessageHandler<RemoveBotFromFlatMessage>
{
    public async ValueTask HandleAsync(
        RemoveBotFromFlatMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
