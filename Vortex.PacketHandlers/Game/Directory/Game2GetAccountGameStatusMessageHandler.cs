using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Game.Directory;

namespace Vortex.PacketHandlers.Game.Directory;

public class Game2GetAccountGameStatusMessageHandler
    : IMessageHandler<Game2GetAccountGameStatusMessage>
{
    public async ValueTask HandleAsync(
        Game2GetAccountGameStatusMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
