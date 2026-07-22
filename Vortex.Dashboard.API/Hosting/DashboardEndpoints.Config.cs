using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Vortex.Dashboard.API.Api;
using Vortex.Dashboard.API.Operations;
using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Hosting;

/// <summary>
/// Server-config editor surface: read the known-key catalog with live values, and set a single key.
/// The write is gated to a known key and validated against its declared kind server-side (see
/// <c>DashboardOperationsService.Config.cs</c>); the grain applies it write-through so the change is
/// live for every reader without an emulator restart.
/// </summary>
internal static partial class DashboardEndpoints
{
    private const string TagConfig = "Config";
    private const string ApiConfig = ApiV1 + "/config";

    public static void MapConfigReads(WebApplication app)
    {
        MapReadGet(
            app,
            ApiConfig,
            "/api/config",
            (DashboardApiService api, CancellationToken ct) => OkAsync(api.ConfigListAsync(ct)),
            Capabilities.Dashboard.ConfigRead,
            TagConfig
        );
    }

    public static void MapConfigOperations(WebApplication app)
    {
        MapPost(
            app,
            ApiOperations + "/config",
            "/api/ops/config",
            async (
                HttpContext ctx,
                SetConfigRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (
                    body is null
                    || string.IsNullOrWhiteSpace(body.Key)
                    || body.Value is null
                    || !HasReason(body.Reason)
                )
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.SetConfigAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsConfigManage,
            TagConfig
        );
    }
}
