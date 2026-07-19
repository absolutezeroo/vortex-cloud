using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Game.Directory;

namespace Vortex.PacketHandlers.Game.Directory;

public class Game2QuickJoinGameMessageHandler : IMessageHandler<Game2QuickJoinGameMessage>
{
    public async ValueTask HandleAsync(
        Game2QuickJoinGameMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
