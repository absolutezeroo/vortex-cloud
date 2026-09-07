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
using Vortex.Dashboard.API.Api.Platform;
using Vortex.Dashboard.API.Api.Platform.Contracts;
using Vortex.Dashboard.API.Infrastructure;
using Vortex.Dashboard.API.Operations;
using Vortex.Dashboard.API.Security;
using Vortex.Observability.Runtime;
using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Hosting;

internal static partial class DashboardEndpoints
{
    public static void MapMonitoringReads(WebApplication app, Func<DateTime> startedAtUtc)
    {
        MapReadGet<DashboardOverview>(
            app,
            ApiMonitoring + "/overview",
            (DashboardMonitoringReads api, CancellationToken ct) =>
                OkAsync(api.OverviewAsync(startedAtUtc(), ct)),
            Capabilities.Dashboard.OverviewRead,
            TagMonitoring
        );
        MapReadGet<InfrastructureHealthSnapshot>(
            app,
            ApiMonitoring + "/infrastructure",
            async (DashboardMonitoringReads api, CancellationToken ct) =>
                Results.Ok(await api.InfrastructureAsync(ct).ConfigureAwait(false)),
            Capabilities.Dashboard.OverviewRead,
            TagMonitoring
        );
        MapReadGet<IncidentDetectionSnapshot>(
            app,
            ApiMonitoring + "/incidents",
            async (DashboardMonitoringReads api, CancellationToken ct) =>
                Results.Ok(await api.IncidentsAsync(ct).ConfigureAwait(false)),
            Capabilities.Dashboard.OverviewRead,
            TagMonitoring
        );
        MapReadGet<PacketStats>(
            app,
            ApiMonitoring + "/packet-stats",
            (DashboardMonitoringReads api, CancellationToken ct) =>
                OkAsync(api.PacketStatsAsync(ct)),
            Capabilities.Dashboard.OverviewRead,
            TagMonitoring
        );
        MapReadGet<RoomPerformanceSnapshot>(
            app,
            ApiMonitoring + "/room-performance",
            (DashboardMonitoringReads api) => Results.Ok(api.RoomPerformance()),
            Capabilities.Dashboard.PerformanceRead,
            TagMonitoring
        );
    }
}
