using System.Collections.Immutable;
using System.Threading.Tasks;
using Vortex.Primitives.Server;
using Vortex.Primitives.Server.Grains;

namespace Vortex.Rooms.Grains.Systems.Banzai;

/// <summary>
/// Admin-editable config keys and defaults for Battle Banzai balance, served live from
/// <see cref="IServerConfigGrain"/> (same pattern as <see cref="Freeze.FreezeConfig"/>). Resolved
/// in one grain round trip per round start. Mirrored (with a guard test) in
/// <c>ConfigKeyCatalog</c>, group "Banzai".
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

    public static async Task<BanzaiSettings> ResolveAsync(IServerConfigGrain config)
    {
        BanzaiSettings d = BanzaiSettings.Default;
        ImmutableDictionary<string, string> v = await config.GetManyAsync(AllKeys);

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
