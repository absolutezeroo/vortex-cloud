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
    public static void MapMonitoringReads(WebApplication app, Func<DateTime> startedAtUtc)
    {
        MapReadGet(
            app,
            ApiMonitoring + "/overview",
            "/api/overview",
            (DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.OverviewAsync(startedAtUtc(), ct)),
            Capabilities.Dashboard.OverviewRead,
            TagMonitoring
        );
        MapReadGet(
            app,
            ApiMonitoring + "/infrastructure",
            "/api/infrastructure",
            async (DashboardApiService api, CancellationToken ct) =>
                Results.Ok(await api.InfrastructureAsync(ct).ConfigureAwait(false)),
            Capabilities.Dashboard.OverviewRead,
            TagMonitoring
        );
        MapReadGet(
            app,
            ApiMonitoring + "/incidents",
            "/api/incidents",
            async (DashboardApiService api, CancellationToken ct) =>
                Results.Ok(await api.IncidentsAsync(ct).ConfigureAwait(false)),
            Capabilities.Dashboard.OverviewRead,
            TagMonitoring
        );
        MapReadGet(
            app,
            ApiMonitoring + "/packet-stats",
            "/api/packet-stats",
            (DashboardApiService api, CancellationToken ct) => OkAsync(api.PacketStatsAsync(ct)),
            Capabilities.Dashboard.OverviewRead,
            TagMonitoring
        );
    }
}
