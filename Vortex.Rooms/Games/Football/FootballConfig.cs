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
    public const string TopPaceKey = "football.top_pace";
    public const string AvatarStopChancePercentKey = "football.avatar_stop_chance_percent";
    public const string GoalPointsKey = "football.goal_points";
    public const string GoalResetMsKey = "football.goal_reset_ms";

    public static readonly ImmutableArray<string> AllKeys =
    [
        KickDistanceKey,
        DragDistanceKey,
        TackleDistanceKey,
        TopPaceKey,
        AvatarStopChancePercentKey,
        GoalPointsKey,
        GoalResetMsKey,
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
            TopPace = ServerConfigValues.GetInt(v, TopPaceKey, d.TopPace),
            AvatarStopChancePercent = ServerConfigValues.GetInt(
                v,
                AvatarStopChancePercentKey,
                d.AvatarStopChancePercent
            ),
            GoalPoints = ServerConfigValues.GetInt(v, GoalPointsKey, d.GoalPoints),
            GoalResetMs = ServerConfigValues.GetInt(v, GoalResetMsKey, d.GoalResetMs),
        };
    }
}
