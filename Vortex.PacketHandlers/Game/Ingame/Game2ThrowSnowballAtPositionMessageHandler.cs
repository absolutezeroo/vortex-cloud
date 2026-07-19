using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Game.Ingame;

namespace Vortex.PacketHandlers.Game.Ingame;

public class Game2ThrowSnowballAtPositionMessageHandler
    : IMessageHandler<Game2ThrowSnowballAtPositionMessage>
{
    public async ValueTask HandleAsync(
        Game2ThrowSnowballAtPositionMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
