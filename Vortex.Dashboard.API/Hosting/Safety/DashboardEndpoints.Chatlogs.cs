using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Vortex.Dashboard.API.Api;
using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Hosting;

/// <summary>
/// Chat search. One route, its own capability: every other chat view in the dashboard hangs off a
/// room or a player an operator already had reason to open, while this one reads across the hotel
/// from a word. Each call lands in the audit trail like every other <c>/api/</c> read, which for
/// this surface is the point rather than a side effect.
/// </summary>
internal static partial class DashboardEndpoints
{
    private const string TagChatlogs = "Chatlogs";
    private const string ApiChatlogs = ApiV1 + "/chatlogs";

    public static void MapChatlogReads(WebApplication app)
    {
        MapReadGet(
            app,
            ApiChatlogs,
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.ChatlogsAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.ChatlogsRead,
            TagChatlogs
        );
    }
}
