using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Game.Arena;

namespace Vortex.PacketHandlers.Game.Arena;

public class Game2LoadStageReadyMessageHandler : IMessageHandler<Game2LoadStageReadyMessage>
{
    public async ValueTask HandleAsync(
        Game2LoadStageReadyMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
