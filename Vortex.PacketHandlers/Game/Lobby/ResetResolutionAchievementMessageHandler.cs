using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Game.Lobby;

namespace Vortex.PacketHandlers.Game.Lobby;

public class ResetResolutionAchievementMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<ResetResolutionAchievementMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        ResetResolutionAchievementMessage message,
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
            .ResetAsync(message.StuffId, ct)
            .ConfigureAwait(false);
    }
}
