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
    public static void MapAuditReads(WebApplication app)
    {
        MapReadGet(
            app,
            ApiForensics + "/audit",
            "/api/audit",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.AuditAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.AuditRead,
            TagForensics
        );
        MapReadGet(
            app,
            ApiForensics + "/moderation/stats",
            "/api/moderation-stats",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.ModerationStatsAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.AuditRead,
            TagForensics
        );
    }
}
