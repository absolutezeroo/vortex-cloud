namespace Vortex.Rooms.Games.BattleBanzai;

/// <summary>
/// The tunable balance of a Battle Banzai round, resolved once per round from server config by
/// <see cref="BanzaiConfig.ResolveAsync"/>. Defaults mirror Arcturus: locking is the only scoring
/// act by default (fill/hijack are worth nothing until an admin says otherwise).
/// </summary>
public sealed record BanzaiSettings
{
    public static readonly BanzaiSettings Default = new();

    /// <summary>Points per tile locked — the stepped tile and every tile of an enclosed region.</summary>
    public int PointsLockTile { get; init; } = 1;

    /// <summary>Points for advancing your own tile's claim (first and second step).</summary>
    public int PointsFillTile { get; init; } = 0;

    /// <summary>Points for stealing a neutral or enemy (unlocked) tile.</summary>
    public int PointsHijackTile { get; init; } = 0;

    public int MaxPlayersPerTeam { get; init; } = 5;

    /// <summary>How many enclosed-region tiles are painted per room tick — the wired event queue is
    /// bounded (512 cap, 64 drained per tick) and every state change publishes into it, so a huge
    /// fill is spread over ticks instead of flooding the queue.</summary>
    public int LockBatchPerTick { get; init; } = 32;
}
