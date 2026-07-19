using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Game.Score;

namespace Vortex.PacketHandlers.Game.Score;

public class GetFriendsWeeklyCompetitiveLeaderboardMessageHandler
    : IMessageHandler<GetFriendsWeeklyCompetitiveLeaderboardMessage>
{
    public async ValueTask HandleAsync(
        GetFriendsWeeklyCompetitiveLeaderboardMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
