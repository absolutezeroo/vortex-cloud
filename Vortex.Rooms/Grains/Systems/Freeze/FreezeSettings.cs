namespace Vortex.Rooms.Grains.Systems.Freeze;

/// <summary>
/// The resolved, tunable balance values for one Freeze game. These are admin-editable and served live
/// from <c>IServerConfigGrain</c> (see <see cref="FreezeConfig"/>); the wrapper resolves them once per
/// round and hands this immutable snapshot to the IO-free game state, so the POCO never touches config.
/// Only the wire-fixed values (effect ids, animation timing) stay in <see cref="FreezeConstants"/>.
/// </summary>
public sealed record FreezeSettings
{
    public int StartLives { get; init; } = 3;
    public int MaxLives { get; init; } = 5;
    public int StartSnowballs { get; init; } = 1;
    public int MaxSnowballs { get; init; } = 5;

    /// <summary>Game ticks (1s each) to regenerate one snowball while below the max; 0 disables regen.</summary>
    public int SnowballRegenTicks { get; init; } = 2;

    public int MaxExplosionBoost { get; init; } = 5;

    public int FrozenTicks { get; init; } = 5;
    public int ProtectionTicks { get; init; } = 12;

    public int FreezeLoseSnowballs { get; init; } = 1;
    public int FreezeLoseBoost { get; init; } = 1;

    public int PowerUpChancePercent { get; init; } = 25;
    public bool ProtectionStacks { get; init; }

    public int FreezePlayerPoints { get; init; } = 10;
    public int DestroyBlockPoints { get; init; } = 5;
    public int PowerUpPoints { get; init; } = 5;

    public int MaxPlayersPerTeam { get; init; } = 5;

    /// <summary>The compiled-in defaults, used before config is resolved and as the config fallback.</summary>
    public static FreezeSettings Default { get; } = new();
}
