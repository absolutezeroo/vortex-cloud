using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Protocol.Messages.Incoming.Quest;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Quest;

/// <summary>
/// The community goal's leaderboard. How many places to send is configuration passed down to the
/// grain, not a constant inside it — a hotel of fifty and a hotel of fifty thousand want different
/// answers.
/// </summary>
public class GetCommunityGoalHallOfFameMessageHandler(
    IGrainFactory grainFactory,
    IConfiguration configuration
) : IMessageHandler<GetCommunityGoalHallOfFameMessage>
{
    private const int DefaultHallOfFameSize = 20;

    public async ValueTask HandleAsync(
        GetCommunityGoalHallOfFameMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        int limit = configuration.GetValue(
            "Vortex:Quests:CommunityGoalHallOfFameSize",
            DefaultHallOfFameSize
        );

        await grainFactory
            .GetCommunityGoalGrain()
            .SendHallOfFameAsync(ctx.PlayerId, limit, ct)
            .ConfigureAwait(false);
    }
}
