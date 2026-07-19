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
    public static void MapFurnitureReads(WebApplication app)
    {
        MapReadGet(
            app,
            ApiFurniture + "/definitions",
            "/api/furniture/definitions",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.FurnitureDefinitionAdminListAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.FurnitureRead,
            TagFurniture
        );
    }

    public static void MapFurnitureOperations(WebApplication app)
    {
        MapPost(
            app,
            ApiOperations + "/furniture/definitions",
            "/api/ops/furniture/definitions",
            async (
                HttpContext ctx,
                CreateFurnitureDefinitionRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (body is null || string.IsNullOrWhiteSpace(body.Name) || !HasReason(body.Reason))
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.CreateFurnitureDefinitionAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsFurnitureManage,
            TagFurniture
        );
        MapPost(
            app,
            ApiOperations + "/furniture/definitions/update",
            "/api/ops/furniture/definitions/update",
            async (
                HttpContext ctx,
                UpdateFurnitureDefinitionRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (
                    body is null
                    || body.DefinitionId <= 0
                    || string.IsNullOrWhiteSpace(body.Name)
                    || !HasReason(body.Reason)
                )
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.UpdateFurnitureDefinitionAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsFurnitureManage,
            TagFurniture
        );
        MapPost(
            app,
            ApiOperations + "/furniture/definitions/delete",
            "/api/ops/furniture/definitions/delete",
            async (
                HttpContext ctx,
                DeleteFurnitureDefinitionRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (body is null || body.DefinitionId <= 0 || !HasReason(body.Reason))
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.DeleteFurnitureDefinitionAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsFurnitureManage,
            TagFurniture
        );
    }
}
