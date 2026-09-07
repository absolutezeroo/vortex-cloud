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
using Vortex.Dashboard.API.Api.Catalogue;
using Vortex.Dashboard.API.Infrastructure;
using Vortex.Dashboard.API.Operations;
using Vortex.Dashboard.API.Operations.Catalogue;
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
            (HttpContext ctx, FurnitureReads reads, CancellationToken ct) =>
                OkAsync(reads.FurnitureDefinitionAdminListAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.FurnitureRead,
            TagFurniture
        );
    }

    public static void MapFurnitureOperations(WebApplication app)
    {
        MapPost(
            app,
            ApiOperations + "/furniture/definitions",
            async (
                HttpContext ctx,
                CreateFurnitureDefinitionRequest body,
                FurnitureOperations ops,
                CancellationToken ct
            ) =>
            {
                if (string.IsNullOrWhiteSpace(body.Name))
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
            async (
                HttpContext ctx,
                UpdateFurnitureDefinitionRequest body,
                FurnitureOperations ops,
                CancellationToken ct
            ) =>
            {
                if (body.DefinitionId <= 0 || string.IsNullOrWhiteSpace(body.Name))
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
            async (
                HttpContext ctx,
                DeleteFurnitureDefinitionRequest body,
                FurnitureOperations ops,
                CancellationToken ct
            ) =>
            {
                if (body.DefinitionId <= 0)
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
