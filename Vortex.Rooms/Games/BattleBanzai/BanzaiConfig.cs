using System.Collections.Immutable;
using System.Threading.Tasks;
using Vortex.Primitives.Server;
using Vortex.Rooms.Games.Abstractions;

namespace Vortex.Rooms.Games.BattleBanzai;

/// <summary>
/// Admin-editable config keys and defaults for Battle Banzai balance, served live from server
/// config (same pattern as <c>FreezeConfig</c>). Resolved in ONE grain round trip per match start,
/// which is the one place in a match's life a round trip is affordable. Mirrored (with a guard
/// test) in <c>ConfigKeyCatalog</c>, group "Banzai".
/// </summary>
public static class BanzaiConfig
{
    public const string PointsLockTileKey = "banzai.points_lock_tile";
    public const string PointsFillTileKey = "banzai.points_fill_tile";
    public const string PointsHijackTileKey = "banzai.points_hijack_tile";
    public const string MaxPlayersPerTeamKey = "banzai.max_players_per_team";
    public const string LockBatchPerTickKey = "banzai.lock_batch_per_tick";

    public static readonly ImmutableArray<string> AllKeys =
    [
        PointsLockTileKey,
        PointsFillTileKey,
        PointsHijackTileKey,
        MaxPlayersPerTeamKey,
        LockBatchPerTickKey,
    ];

    public static async Task<BanzaiSettings> ResolveAsync(IRoomGameContext context)
    {
        BanzaiSettings d = BanzaiSettings.Default;
        ImmutableDictionary<string, string> v = await context.GetConfigAsync(AllKeys);

        return new BanzaiSettings
        {
            PointsLockTile = ServerConfigValues.GetInt(v, PointsLockTileKey, d.PointsLockTile),
            PointsFillTile = ServerConfigValues.GetInt(v, PointsFillTileKey, d.PointsFillTile),
            PointsHijackTile = ServerConfigValues.GetInt(
                v,
                PointsHijackTileKey,
                d.PointsHijackTile
            ),
            MaxPlayersPerTeam = ServerConfigValues.GetInt(
                v,
                MaxPlayersPerTeamKey,
                d.MaxPlayersPerTeam
            ),
            LockBatchPerTick = ServerConfigValues.GetInt(
                v,
                LockBatchPerTickKey,
                d.LockBatchPerTick
            ),
        };
    }
}
