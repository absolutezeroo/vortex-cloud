using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Game.Directory;

namespace Vortex.PacketHandlers.Game.Directory;

public class Game2CheckGameDirectoryStatusMessageHandler
    : IMessageHandler<Game2CheckGameDirectoryStatusMessage>
{
    public async ValueTask HandleAsync(
        Game2CheckGameDirectoryStatusMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
