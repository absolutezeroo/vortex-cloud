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
using Turbo.Dashboard.API.Api;
using Turbo.Dashboard.API.Infrastructure;
using Turbo.Dashboard.API.Operations;
using Turbo.Dashboard.API.Security;
using Turbo.Primitives.Permissions;

namespace Turbo.Dashboard.API.Hosting;

internal static partial class DashboardEndpoints
{
    public static void MapDirectoryReads(WebApplication app)
    {
        MapReadGet(
            app,
            ApiDirectory + "/search",
            "/api/search",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.SearchAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.AuditRead,
            TagForensics
        );
        MapReadGet(
            app,
            ApiDirectory + "/players",
            "/api/players",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.PlayersAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.PlayersRead,
            TagDirectory
        );
        MapReadGet(
            app,
            ApiDirectory + "/furniture",
            "/api/furniture",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.FurnitureDefinitionsAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.FurnitureRead,
            TagDirectory
        );
        MapReadGet(
            app,
            ApiDirectory + "/entity/{id}",
            "/api/item/{id}",
            (string id, HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkNullableAsync(api.ItemAsync(id, ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.AuditRead,
            TagForensics
        );
        MapReadGet(
            app,
            ApiDirectory + "/rooms/{roomId:int}",
            "/api/room/{roomId:int}",
            (int roomId, HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkNullableAsync(api.RoomTimelineAsync(roomId, ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.AuditRead,
            TagForensics
        );
    }
}
