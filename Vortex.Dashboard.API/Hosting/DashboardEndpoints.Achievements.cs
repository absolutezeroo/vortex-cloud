using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Vortex.Dashboard.API.Api;
using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Hosting;

/// <summary>
/// Read-only achievement surface: the definition catalogue with its level ladder, one definition's
/// player distribution, and hotel-wide progression health. Achievement definitions are seeded rather
/// than authored in the dashboard, so there is no operations half here.
/// </summary>
internal static partial class DashboardEndpoints
{
    private const string TagAchievements = "Achievements";
    private const string ApiAchievements = ApiV1 + "/achievements";

    public static void MapAchievementReads(WebApplication app)
    {
        MapReadGet(
            app,
            ApiAchievements,
            "/api/achievements",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.AchievementsAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.AchievementsRead,
            TagAchievements
        );
        MapReadGet(
            app,
            ApiAchievements + "/stats",
            "/api/achievements/stats",
            (DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.AchievementsStatsAsync(ct)),
            Capabilities.Dashboard.AchievementsRead,
            TagAchievements
        );
        MapReadGet(
            app,
            ApiAchievements + "/{achievementId:int}",
            "/api/achievements/{achievementId:int}",
            (int achievementId, DashboardApiService api, CancellationToken ct) =>
                OkNullableAsync(api.AchievementDetailAsync(achievementId, ct)),
            Capabilities.Dashboard.AchievementsRead,
            TagAchievements
        );
    }
}
