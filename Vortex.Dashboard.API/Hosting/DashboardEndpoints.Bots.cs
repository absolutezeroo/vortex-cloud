using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Vortex.Dashboard.API.Api;
using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Hosting;

/// <summary>
/// Read-only surface for bots and hand items. Bots are edited from inside the game (the client's own
/// bot menu is the authoring tool), so the dashboard observes rather than writes.
/// </summary>
internal static partial class DashboardEndpoints
{
    private const string TagBots = "Bots";
    private const string ApiBots = ApiV1 + "/bots";
    private const string ApiHandItems = ApiV1 + "/hand-items";

    public static void MapBotReads(WebApplication app)
    {
        MapReadGet(
            app,
            ApiBots,
            "/api/bots",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.BotsAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.BotsRead,
            TagBots
        );
        MapReadGet(
            app,
            ApiBots + "/stats",
            "/api/bots/stats",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.BotsStatsAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.BotsRead,
            TagBots
        );
        MapReadGet(
            app,
            ApiBots + "/{botId:int}",
            "/api/bots/{botId:int}",
            (int botId, DashboardApiService api, CancellationToken ct) =>
                OkNullableAsync(api.BotDetailAsync(botId, ct)),
            Capabilities.Dashboard.BotsRead,
            TagBots
        );
        MapReadGet(
            app,
            ApiHandItems,
            "/api/hand-items",
            (DashboardApiService api, CancellationToken ct) => OkAsync(api.HandItemsAsync(ct)),
            Capabilities.Dashboard.BotsRead,
            TagBots
        );
    }
}
