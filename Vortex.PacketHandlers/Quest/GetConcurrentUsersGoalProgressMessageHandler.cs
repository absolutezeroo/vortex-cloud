using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Quest;

namespace Vortex.PacketHandlers.Quest;

/// <summary>
/// The landing view polls this every 5 seconds while its "players online" widget is visible. The
/// live count comes from the session gateway here rather than inside the grain: how many people are
/// connected is a session-layer fact, and passing it in keeps the grain free of host concerns.
/// </summary>
public class GetConcurrentUsersGoalProgressMessageHandler(
    IGrainFactory grainFactory,
    ISessionGateway sessionGateway
) : IMessageHandler<GetConcurrentUsersGoalProgressMessage>
{
    public async ValueTask HandleAsync(
        GetConcurrentUsersGoalProgressMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        await grainFactory
            .GetPlayerQuestGrain(ctx.PlayerId)
            .SendConcurrentUsersGoalAsync(sessionGateway.GetOnlinePlayerIds().Count, ct)
            .ConfigureAwait(false);
    }
}
