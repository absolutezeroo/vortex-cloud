using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Game.Score;

namespace Vortex.PacketHandlers.Game.Score;

public class Game2GetWeeklyGroupLeaderboardMessageHandler
    : IMessageHandler<Game2GetWeeklyGroupLeaderboardMessage>
{
    public async ValueTask HandleAsync(
        Game2GetWeeklyGroupLeaderboardMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
