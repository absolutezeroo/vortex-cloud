using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Orleans.Concurrency;

namespace Vortex.Primitives.Players.Grains;

/// <summary>
/// The resolution statues a player owns: which challenge is running on each, and how it ends. Every
/// method sends its own composer — the handlers only forward.
/// </summary>
public interface IPlayerAchievementResolutionGrain : IGrainWithIntegerKey
{
    /// <summary>
    /// Answers a click on a statue, and takes on a challenge when <paramref name="achievementId"/>
    /// is non-zero. One method because the client sends one message for both: opening the statue is
    /// the same packet with a zero.
    /// </summary>
    public Task OpenAsync(int stuffId, int achievementId, CancellationToken ct);

    /// <summary>Abandons the challenge running on this statue and shows the picker again.</summary>
    public Task ResetAsync(int stuffId, CancellationToken ct);

    /// <summary>
    /// Called when the player clears a level, to finish any challenge that was waiting on it.
    /// One-way: achievement progression must not slow down or fail because of a statue.
    /// </summary>
    [OneWay]
    public Task OnAchievementLevelUpAsync(
        int achievementId,
        int completedLevels,
        CancellationToken ct
    );
}
