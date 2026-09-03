using System.Collections.Immutable;
using System.Threading.Tasks;
using Vortex.Primitives.Server;
using Vortex.Rooms.Games.Abstractions;

namespace Vortex.Rooms.Games.Freeze;

/// <summary>
/// Admin-editable config keys and defaults for the Freeze game balance, served live from server
/// config (the same pattern as GroupConfig / ClubConfig). Each key's default is the fallback when no
/// admin override is stored. <see cref="ResolveAsync"/> reads the whole set into an immutable
/// <see cref="FreezeSettings"/> in ONE grain round trip per match.
/// </summary>
public static class FreezeConfig
{
    public const string StartLivesKey = "freeze.start_lives";
    public const string MaxLivesKey = "freeze.max_lives";
    public const string StartSnowballsKey = "freeze.start_snowballs";
    public const string MaxSnowballsKey = "freeze.max_snowballs";
    public const string SnowballRegenTicksKey = "freeze.snowball_regen_ticks";
    public const string MaxBoostKey = "freeze.max_explosion_boost";
    public const string FrozenTicksKey = "freeze.frozen_ticks";
    public const string ProtectionTicksKey = "freeze.protection_ticks";
    public const string LoseSnowballsKey = "freeze.freeze_lose_snowballs";
    public const string LoseBoostKey = "freeze.freeze_lose_boost";
    public const string PowerUpChanceKey = "freeze.powerup_chance_percent";
    public const string ProtectionStacksKey = "freeze.protection_stacks";
    public const string FreezePointsKey = "freeze.points_freeze_player";
    public const string BlockPointsKey = "freeze.points_destroy_block";
    public const string PowerUpPointsKey = "freeze.points_powerup";
    public const string MaxPlayersPerTeamKey = "freeze.max_players_per_team";

    /// <summary>Every key of the group, for the one-round-trip batch resolve.</summary>
    public static readonly ImmutableArray<string> AllKeys =
    [
        StartLivesKey,
        MaxLivesKey,
        StartSnowballsKey,
        MaxSnowballsKey,
        SnowballRegenTicksKey,
        MaxBoostKey,
        FrozenTicksKey,
        ProtectionTicksKey,
        LoseSnowballsKey,
        LoseBoostKey,
        PowerUpChanceKey,
        ProtectionStacksKey,
        FreezePointsKey,
        BlockPointsKey,
        PowerUpPointsKey,
        MaxPlayersPerTeamKey,
    ];

    /// <summary>Reads the live balance from the server config in a single grain round trip (this runs
    /// on the critical path of every round start), falling back to the compiled defaults.</summary>
    public static async Task<FreezeSettings> ResolveAsync(IRoomGameContext context)
    {
        FreezeSettings d = FreezeSettings.Default;
        ImmutableDictionary<string, string> v = await context.GetConfigAsync(AllKeys);

        return new FreezeSettings
        {
            StartLives = ServerConfigValues.GetInt(v, StartLivesKey, d.StartLives),
            MaxLives = ServerConfigValues.GetInt(v, MaxLivesKey, d.MaxLives),
            StartSnowballs = ServerConfigValues.GetInt(v, StartSnowballsKey, d.StartSnowballs),
            MaxSnowballs = ServerConfigValues.GetInt(v, MaxSnowballsKey, d.MaxSnowballs),
            SnowballRegenTicks = ServerConfigValues.GetInt(
                v,
                SnowballRegenTicksKey,
                d.SnowballRegenTicks
            ),
            MaxExplosionBoost = ServerConfigValues.GetInt(v, MaxBoostKey, d.MaxExplosionBoost),
            FrozenTicks = ServerConfigValues.GetInt(v, FrozenTicksKey, d.FrozenTicks),
            ProtectionTicks = ServerConfigValues.GetInt(v, ProtectionTicksKey, d.ProtectionTicks),
            FreezeLoseSnowballs = ServerConfigValues.GetInt(
                v,
                LoseSnowballsKey,
                d.FreezeLoseSnowballs
            ),
            FreezeLoseBoost = ServerConfigValues.GetInt(v, LoseBoostKey, d.FreezeLoseBoost),
            PowerUpChancePercent = ServerConfigValues.GetInt(
                v,
                PowerUpChanceKey,
                d.PowerUpChancePercent
            ),
            ProtectionStacks = ServerConfigValues.GetBool(
                v,
                ProtectionStacksKey,
                d.ProtectionStacks
            ),
            FreezePlayerPoints = ServerConfigValues.GetInt(
                v,
                FreezePointsKey,
                d.FreezePlayerPoints
            ),
            DestroyBlockPoints = ServerConfigValues.GetInt(v, BlockPointsKey, d.DestroyBlockPoints),
            PowerUpPoints = ServerConfigValues.GetInt(v, PowerUpPointsKey, d.PowerUpPoints),
            MaxPlayersPerTeam = ServerConfigValues.GetInt(
                v,
                MaxPlayersPerTeamKey,
                d.MaxPlayersPerTeam
            ),
        };
    }
}
