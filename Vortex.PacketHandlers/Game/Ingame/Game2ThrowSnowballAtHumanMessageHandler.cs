using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Game.Ingame;

namespace Vortex.PacketHandlers.Game.Ingame;

public class Game2ThrowSnowballAtHumanMessageHandler
    : IMessageHandler<Game2ThrowSnowballAtHumanMessage>
{
    public async ValueTask HandleAsync(
        Game2ThrowSnowballAtHumanMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
