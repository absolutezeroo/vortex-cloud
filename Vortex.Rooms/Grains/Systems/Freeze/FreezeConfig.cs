using System.Threading.Tasks;
using Vortex.Primitives.Server.Grains;

namespace Vortex.Rooms.Grains.Systems.Freeze;

/// <summary>
/// Admin-editable config keys and defaults for the Freeze game balance, served live from
/// <see cref="IServerConfigGrain"/> (the same pattern as GroupConfig / ClubConfig). Each key's default
/// is the fallback when no admin override is stored. <see cref="ResolveAsync"/> reads the whole set
/// into an immutable <see cref="FreezeSettings"/> once per round.
/// </summary>
public static class FreezeConfig
{
    public const string StartLivesKey = "freeze.start_lives";
    public const string MaxLivesKey = "freeze.max_lives";
    public const string StartSnowballsKey = "freeze.start_snowballs";
    public const string MaxSnowballsKey = "freeze.max_snowballs";
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

    /// <summary>Reads the live balance from the server config, falling back to the compiled defaults.</summary>
    public static async Task<FreezeSettings> ResolveAsync(IServerConfigGrain config)
    {
        FreezeSettings d = FreezeSettings.Default;

        return new FreezeSettings
        {
            StartLives = await config.GetIntAsync(StartLivesKey, d.StartLives),
            MaxLives = await config.GetIntAsync(MaxLivesKey, d.MaxLives),
            StartSnowballs = await config.GetIntAsync(StartSnowballsKey, d.StartSnowballs),
            MaxSnowballs = await config.GetIntAsync(MaxSnowballsKey, d.MaxSnowballs),
            MaxExplosionBoost = await config.GetIntAsync(MaxBoostKey, d.MaxExplosionBoost),
            FrozenTicks = await config.GetIntAsync(FrozenTicksKey, d.FrozenTicks),
            ProtectionTicks = await config.GetIntAsync(ProtectionTicksKey, d.ProtectionTicks),
            FreezeLoseSnowballs = await config.GetIntAsync(LoseSnowballsKey, d.FreezeLoseSnowballs),
            FreezeLoseBoost = await config.GetIntAsync(LoseBoostKey, d.FreezeLoseBoost),
            PowerUpChancePercent = await config.GetIntAsync(
                PowerUpChanceKey,
                d.PowerUpChancePercent
            ),
            ProtectionStacks = await config.GetBoolAsync(ProtectionStacksKey, d.ProtectionStacks),
            FreezePlayerPoints = await config.GetIntAsync(FreezePointsKey, d.FreezePlayerPoints),
            DestroyBlockPoints = await config.GetIntAsync(BlockPointsKey, d.DestroyBlockPoints),
            PowerUpPoints = await config.GetIntAsync(PowerUpPointsKey, d.PowerUpPoints),
            MaxPlayersPerTeam = await config.GetIntAsync(MaxPlayersPerTeamKey, d.MaxPlayersPerTeam),
        };
    }
}
