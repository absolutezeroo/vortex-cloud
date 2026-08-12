using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Quest;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Quest;

/// <summary>
/// The daily-tasks window asking for the player's board. How many tasks a board holds is
/// configuration passed down to the grain rather than a constant inside it.
/// </summary>
public class GetDailyTasksMessageHandler(IGrainFactory grainFactory, IConfiguration configuration)
    : IMessageHandler<GetDailyTasksMessage>
{
    private const int DefaultTaskCount = 3;
    private const int DefaultBonusCount = 1;

    public async ValueTask HandleAsync(
        GetDailyTasksMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        int taskCount = configuration.GetValue("Vortex:Quests:DailyTaskCount", DefaultTaskCount);
        int bonusCount = configuration.GetValue(
            "Vortex:Quests:DailyBonusTaskCount",
            DefaultBonusCount
        );

        await grainFactory
            .GetPlayerDailyTaskGrain(ctx.PlayerId)
            .SendBoardAsync(taskCount, bonusCount, ct)
            .ConfigureAwait(false);
    }
}
