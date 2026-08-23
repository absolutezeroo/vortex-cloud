using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Quest;

namespace Vortex.PacketHandlers.Quest;

/// <summary>
/// The landing view asks where the hotel stands on the community goal. The grain owns the reply —
/// it resolves which goal is active and sends the widget state itself.
/// </summary>
public class GetCommunityGoalProgressMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetCommunityGoalProgressMessage>
{
    public async ValueTask HandleAsync(
        GetCommunityGoalProgressMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        await grainFactory
            .GetCommunityGoalGrain()
            .SendProgressAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);
    }
}
