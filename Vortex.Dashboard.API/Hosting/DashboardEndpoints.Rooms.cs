using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Vortex.Dashboard.API.Api;
using Vortex.Dashboard.API.Infrastructure;
using Vortex.Dashboard.API.Operations;
using Vortex.Dashboard.API.Security;
using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Hosting;

internal static partial class DashboardEndpoints
{
    public static void MapRoomReads(WebApplication app)
    {
        MapReadGet(
            app,
            ApiDirectory + "/rooms/active",
            "/api/rooms/active",
            async (DashboardOperationsService ops, CancellationToken ct) =>
                Results.Ok(await ops.GetActiveRoomsAsync().ConfigureAwait(false)),
            Capabilities.Dashboard.OpsRoomsManage,
            TagDirectory
        );
        MapReadGet(
            app,
            ApiDirectory + "/rooms/{roomId:int}/occupants",
            "/api/rooms/{roomId:int}/occupants",
            async (int roomId, DashboardOperationsService ops, CancellationToken ct) =>
                Results.Ok(await ops.GetRoomOccupantsAsync(roomId, ct).ConfigureAwait(false)),
            Capabilities.Dashboard.OpsRoomsManage,
            TagDirectory
        );
    }

    public static void MapRoomOperations(WebApplication app)
    {
        MapPost(
            app,
            ApiOperations + "/rooms/close",
            "/api/ops/rooms/close",
            async (
                HttpContext ctx,
                ForceCloseRoomRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (body is null || body.RoomId <= 0 || !HasReason(body.Reason))
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.ForceCloseRoomAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsRoomsManage,
            TagOperations
        );
        MapPost(
            app,
            ApiOperations + "/rooms/kick",
            "/api/ops/rooms/kick",
            async (
                HttpContext ctx,
                KickFromRoomRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (
                    body is null
                    || body.RoomId <= 0
                    || body.PlayerId <= 0
                    || !HasReason(body.Reason)
                )
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.KickFromRoomAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsRoomsManage,
            TagOperations
        );
    }
}
