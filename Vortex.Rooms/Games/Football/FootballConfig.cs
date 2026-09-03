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
    public const string BallStepMsKey = "football.ball_step_ms";
    public const string GoalPointsKey = "football.goal_points";
    public const string GoalResetMsKey = "football.goal_reset_ms";
    public const string MaxPlayersPerTeamKey = "football.max_players_per_team";

    public static readonly ImmutableArray<string> AllKeys =
    [
        KickDistanceKey,
        BallStepMsKey,
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
            BallStepMs = ServerConfigValues.GetInt(v, BallStepMsKey, d.BallStepMs),
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
