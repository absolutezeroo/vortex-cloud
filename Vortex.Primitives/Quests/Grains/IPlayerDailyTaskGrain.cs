using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Orleans.Concurrency;

namespace Vortex.Primitives.Quests.Grains;

/// <summary>
/// Per-player daily tasks: drawing the day's board, advancing it, and handing over rewards. Every
/// method owns its outbound composer.
/// </summary>
public interface IPlayerDailyTaskGrain : IGrainWithIntegerKey
{
    /// <summary>
    /// Sends the player's board, drawing today's assignments first if the day turned over.
    /// <paramref name="taskCount"/> and <paramref name="bonusCount"/> come from configuration.
    /// </summary>
    public Task SendBoardAsync(int taskCount, int bonusCount, CancellationToken ct);

    /// <summary>
    /// Advances every assigned, unfinished task whose objective matches, completing those that reach
    /// their required repeats. Driven from the same domain events quests use, hence <c>[OneWay]</c>.
    /// </summary>
    [OneWay]
    public Task ProgressAsync(string questTypeCode, int amount, CancellationToken ct);

    /// <summary>Hands over a completed task's reward. Ignored when the task is not claimable.</summary>
    public Task ClaimAsync(int taskId, CancellationToken ct);
}
