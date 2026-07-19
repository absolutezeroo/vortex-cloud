using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Game.Arena;

namespace Vortex.PacketHandlers.Game.Arena;

public class Game2GameChatMessageHandler : IMessageHandler<Game2GameChatMessage>
{
    public async ValueTask HandleAsync(
        Game2GameChatMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
