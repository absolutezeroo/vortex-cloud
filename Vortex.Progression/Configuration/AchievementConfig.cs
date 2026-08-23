namespace Vortex.Players.Configuration;

/// <summary>
/// Strongly-typed options for achievement progression. Only the write-batching knobs live here:
/// they are infrastructure timing, read once at grain activation to register the flush timer, so
/// they stay bound via IOptions rather than served live from <c>IServerConfigGrain</c>. The tunable
/// achievement content (levels, thresholds, rewards) is admin-editable data in the database.
/// </summary>
public sealed class AchievementConfig
{
    public const string SECTION_NAME = "Vortex:Achievements";

    /// <summary>
    /// Interval between flushes of the progress counters held in memory by
    /// <c>PlayerAchievementGrain</c>. A silo crash costs at most this much progress; level-ups are
    /// never batched, so nothing that hands out a badge or currency is ever at risk.
    /// </summary>
    public int ProgressFlushIntervalMs { get; init; } = 5000;

    /// <summary>Maximum number of achievement rows written per flush.</summary>
    public int MaxDirtyProgressPerFlush { get; init; } = 100;
}
