using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Game.Score;

namespace Vortex.PacketHandlers.Game.Score;

public class GetWeeklyGameRewardMessageHandler : IMessageHandler<GetWeeklyGameRewardMessage>
{
    public async ValueTask HandleAsync(
        GetWeeklyGameRewardMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
