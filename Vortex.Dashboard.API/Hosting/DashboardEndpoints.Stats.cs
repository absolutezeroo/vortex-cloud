using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Vortex.Dashboard.API.Api;
using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Hosting;

/// <summary>Read-only analytics endpoints for domains that previously had zero dashboard visibility
/// (guilds, pets, CFH, catalog purchases, wired furniture) — see the individual
/// <c>DashboardApiService.*.cs</c> partials for what each aggregates and why.</summary>
internal static partial class DashboardEndpoints
{
    private const string TagStats = "Stats";
    private const string ApiGroups = ApiV1 + "/groups";
    private const string ApiPets = ApiV1 + "/pets";
    private const string ApiCfh = ApiV1 + "/cfh";
    private const string ApiWired = ApiV1 + "/wired";

    public static void MapStatsReads(WebApplication app)
    {
        MapReadGet(
            app,
            ApiGroups + "/stats",
            "/api/groups/stats",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.GroupsStatsAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.GroupsRead,
            TagStats
        );
        MapReadGet(
            app,
            ApiPets + "/stats",
            "/api/pets/stats",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.PetsStatsAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.PetsRead,
            TagStats
        );
        MapReadGet(
            app,
            ApiCfh + "/stats",
            "/api/cfh/stats",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.CfhStatsAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.CfhRead,
            TagStats
        );
        MapReadGet(
            app,
            ApiCatalog + "/purchases/stats",
            "/api/catalog/purchases/stats",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.CatalogPurchasesStatsAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.CatalogPurchasesRead,
            TagStats
        );
        MapReadGet(
            app,
            ApiWired + "/stats",
            "/api/wired/stats",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.WiredStatsAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.WiredRead,
            TagStats
        );
    }
}
