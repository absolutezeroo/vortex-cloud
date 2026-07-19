using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Game.Score;

namespace Vortex.PacketHandlers.Game.Score;

public class GetWeeklyCompetitiveLeaderboardMessageHandler
    : IMessageHandler<GetWeeklyCompetitiveLeaderboardMessage>
{
    public async ValueTask HandleAsync(
        GetWeeklyCompetitiveLeaderboardMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
