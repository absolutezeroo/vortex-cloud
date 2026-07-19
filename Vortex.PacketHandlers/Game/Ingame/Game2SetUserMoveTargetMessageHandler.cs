using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Game.Ingame;

namespace Vortex.PacketHandlers.Game.Ingame;

public class Game2SetUserMoveTargetMessageHandler : IMessageHandler<Game2SetUserMoveTargetMessage>
{
    public async ValueTask HandleAsync(
        Game2SetUserMoveTargetMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
