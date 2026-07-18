using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Primitives.Players.Snapshots;

namespace Turbo.Primitives.Players.Grains;

/// <summary>
/// Per-player grain owning that player's achievement progress. Reads resolve each achievement
/// against the cached definitions into wire-ready snapshots.
/// </summary>
public interface IPlayerAchievementGrain : IGrainWithIntegerKey
{
    public Task<AchievementListSnapshot> GetAchievementsAsync(CancellationToken ct);

    /// <summary>
    /// Adds <paramref name="amount"/> progress to the achievement identified by
    /// <paramref name="achievementName"/> (case-insensitive). Handles level-ups end to end: grants
    /// badges and rewards, updates the achievement score, and pushes the resulting composers. No-ops
    /// for an unknown achievement or a non-positive amount.
    /// </summary>
    public Task ProgressAsync(string achievementName, int amount, CancellationToken ct);
}
