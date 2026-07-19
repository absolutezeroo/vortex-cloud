using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Game.Score;

namespace Vortex.PacketHandlers.Game.Score;

public class Game2GetFriendsLeaderboardMessageHandler
    : IMessageHandler<Game2GetFriendsLeaderboardMessage>
{
    public async ValueTask HandleAsync(
        Game2GetFriendsLeaderboardMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
