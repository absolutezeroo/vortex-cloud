using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Vortex.Dashboard.API.Api;
using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Hosting;

/// <summary>
/// Read-only surfaces for the domains that own tables but no admin flow: the social graph and guild
/// forums, the staff/role matrix, the economy's smaller tables (LTD, rentals, currencies, builders
/// club), what players hold (badges/effects/chat styles/outfits), and NFT collections.
/// <para>
/// Grouped in one mapper because none of them writes: every route here is a GET behind a read
/// capability, and the domains that do have operations keep their own file.
/// </para>
/// </summary>
internal static partial class DashboardEndpoints
{
    private const string TagSocial = "Social";
    private const string TagStaff = "Staff";
    private const string TagCollectibles = "Collectibles";
    private const string ApiSocial = ApiV1 + "/social";
    private const string ApiStaff = ApiV1 + "/staff";
    private const string ApiPlayerRewards = ApiV1 + "/player-rewards";
    private const string ApiCollectibles = ApiV1 + "/collectibles";

    public static void MapInsightReads(WebApplication app)
    {
        MapReadGet(
            app,
            ApiSocial + "/stats",
            "/api/social/stats",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.SocialStatsAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.SocialRead,
            TagSocial
        );
        MapReadGet(
            app,
            ApiStaff,
            "/api/staff",
            (DashboardApiService api, CancellationToken ct) => OkAsync(api.StaffAsync(ct)),
            Capabilities.Dashboard.StaffRead,
            TagStaff
        );
        MapReadGet(
            app,
            ApiStaff + "/accounts",
            "/api/staff/accounts",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.StaffAccountSearchAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.StaffRead,
            TagStaff
        );
        MapReadGet(
            app,
            ApiEconomy + "/extras",
            "/api/economy/extras",
            (DashboardApiService api, CancellationToken ct) => OkAsync(api.EconomyExtrasAsync(ct)),
            Capabilities.Dashboard.EconomyRead,
            TagEconomy
        );
        MapReadGet(
            app,
            ApiPlayerRewards,
            "/api/player-rewards",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.PlayerRewardsAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.PlayersRead,
            TagForensics
        );
        MapReadGet(
            app,
            ApiPlayerRewards + "/{playerId:int}",
            "/api/player-rewards/{playerId:int}",
            (int playerId, DashboardApiService api, CancellationToken ct) =>
                OkNullableAsync(api.PlayerRewardDetailAsync(playerId, ct)),
            Capabilities.Dashboard.PlayersRead,
            TagForensics
        );
        MapReadGet(
            app,
            ApiMonitoring + "/inventory",
            "/api/monitoring/inventory",
            (DashboardApiService api, CancellationToken ct) => OkAsync(api.InventoryAsync(ct)),
            Capabilities.Dashboard.OverviewRead,
            TagMonitoring
        );
        MapReadGet(
            app,
            ApiCollectibles,
            "/api/collectibles",
            (DashboardApiService api, CancellationToken ct) => OkAsync(api.CollectiblesAsync(ct)),
            Capabilities.Dashboard.CollectiblesRead,
            TagCollectibles
        );
    }
}
