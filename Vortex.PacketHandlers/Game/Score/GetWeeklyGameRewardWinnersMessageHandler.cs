using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Game.Score;

namespace Vortex.PacketHandlers.Game.Score;

public class GetWeeklyGameRewardWinnersMessageHandler
    : IMessageHandler<GetWeeklyGameRewardWinnersMessage>
{
    public async ValueTask HandleAsync(
        GetWeeklyGameRewardWinnersMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
