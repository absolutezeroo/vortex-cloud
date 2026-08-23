using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.Quest;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Quest;

/// <summary>
/// The player pressed "claim" on the "players online" widget. The grain re-checks the goal against
/// the count read here — the button can be pressed long after the widget last refreshed, by which
/// time the hotel may have emptied out.
/// </summary>
public class GetConcurrentUsersRewardMessageHandler(
    IGrainFactory grainFactory,
    ISessionGateway sessionGateway
) : IMessageHandler<GetConcurrentUsersRewardMessage>
{
    public async ValueTask HandleAsync(
        GetConcurrentUsersRewardMessage message,
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
            .ClaimConcurrentUsersRewardAsync(sessionGateway.GetOnlinePlayerIds().Count, ct)
            .ConfigureAwait(false);
    }
}
