using System.Collections.Immutable;
using System.Threading.Tasks;
using Vortex.Primitives.Server;
using Vortex.Rooms.Games.Abstractions;

namespace Vortex.Rooms.Games.Football;

/// <summary>
/// Admin-editable config keys and defaults for football balance, resolved in ONE grain round trip per
/// match start (the same pattern as <c>FreezeConfig</c> and <c>BanzaiConfig</c>). Mirrored, with a
/// guard test, in <c>ConfigKeyCatalog</c>, group "Football".
/// </summary>
public static class FootballConfig
{
    public const string KickDistanceKey = "football.kick_distance";
    public const string DragDistanceKey = "football.drag_distance";
    public const string TackleDistanceKey = "football.tackle_distance";
    public const string FastStepMsKey = "football.fast_step_ms";
    public const string SlowStepMsKey = "football.slow_step_ms";
    public const string FastStepsKey = "football.fast_steps";
    public const string AvatarStopChancePercentKey = "football.avatar_stop_chance_percent";
    public const string GoalPointsKey = "football.goal_points";
    public const string GoalResetMsKey = "football.goal_reset_ms";
    public const string MaxPlayersPerTeamKey = "football.max_players_per_team";

    public static readonly ImmutableArray<string> AllKeys =
    [
        KickDistanceKey,
        DragDistanceKey,
        TackleDistanceKey,
        FastStepMsKey,
        SlowStepMsKey,
        FastStepsKey,
        AvatarStopChancePercentKey,
        GoalPointsKey,
        GoalResetMsKey,
        MaxPlayersPerTeamKey,
    ];

    public static async Task<FootballSettings> ResolveAsync(IRoomGameContext context)
    {
        FootballSettings d = FootballSettings.Default;
        ImmutableDictionary<string, string> v = await context.GetConfigAsync(AllKeys);

        return new FootballSettings
        {
            KickDistance = ServerConfigValues.GetInt(v, KickDistanceKey, d.KickDistance),
            DragDistance = ServerConfigValues.GetInt(v, DragDistanceKey, d.DragDistance),
            TackleDistance = ServerConfigValues.GetInt(v, TackleDistanceKey, d.TackleDistance),
            FastStepMs = ServerConfigValues.GetInt(v, FastStepMsKey, d.FastStepMs),
            SlowStepMs = ServerConfigValues.GetInt(v, SlowStepMsKey, d.SlowStepMs),
            FastSteps = ServerConfigValues.GetInt(v, FastStepsKey, d.FastSteps),
            AvatarStopChancePercent = ServerConfigValues.GetInt(
                v,
                AvatarStopChancePercentKey,
                d.AvatarStopChancePercent
            ),
            GoalPoints = ServerConfigValues.GetInt(v, GoalPointsKey, d.GoalPoints),
            GoalResetMs = ServerConfigValues.GetInt(v, GoalResetMsKey, d.GoalResetMs),
            MaxPlayersPerTeam = ServerConfigValues.GetInt(
                v,
                MaxPlayersPerTeamKey,
                d.MaxPlayersPerTeam
            ),
        };
    }
}
