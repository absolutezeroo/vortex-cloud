using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.Game.Lobby;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Game.Lobby;

public class GetResolutionAchievementsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetResolutionAchievementsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetResolutionAchievementsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        await _grainFactory
            .GetPlayerAchievementResolutionGrain(ctx.PlayerId)
            .OpenAsync(message.StuffId, message.AchievementId, ct)
            .ConfigureAwait(false);
    }
}
