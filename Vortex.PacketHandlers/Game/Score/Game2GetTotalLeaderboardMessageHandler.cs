using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Game.Score;

namespace Vortex.PacketHandlers.Game.Score;

public class Game2GetTotalLeaderboardMessageHandler
    : IMessageHandler<Game2GetTotalLeaderboardMessage>
{
    public async ValueTask HandleAsync(
        Game2GetTotalLeaderboardMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
