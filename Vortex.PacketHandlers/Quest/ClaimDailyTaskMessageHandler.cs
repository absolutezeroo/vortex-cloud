using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.Quest;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Quest;

/// <summary>
/// Claim a completed daily task's reward. The grain re-checks ownership and status — the id comes
/// straight off the wire and nothing stops it naming someone else's task.
/// </summary>
public class ClaimDailyTaskMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<ClaimDailyTaskMessage>
{
    public async ValueTask HandleAsync(
        ClaimDailyTaskMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.TaskId <= 0)
        {
            return;
        }

        await grainFactory
            .GetPlayerDailyTaskGrain(ctx.PlayerId)
            .ClaimAsync(message.TaskId, ct)
            .ConfigureAwait(false);
    }
}
