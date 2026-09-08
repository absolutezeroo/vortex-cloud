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
using Vortex.Dashboard.API.Api.Safety;
using Vortex.Dashboard.API.Api.Safety.Contracts;
using Vortex.Dashboard.API.Infrastructure;
using Vortex.Dashboard.API.Operations;
using Vortex.Dashboard.API.Security;
using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Hosting;

internal static partial class DashboardEndpoints
{
    public static void MapAuditReads(WebApplication app)
    {
        MapReadGet<AuditPage>(
            app,
            ApiForensics + "/audit",
            (HttpContext ctx, AuditReads reads, CancellationToken ct) =>
                OkAsync(reads.AuditAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.AuditRead,
            TagForensics
        );
        // Same capability as the audit trail it reads from: this is the audit trail, asked a
        // different question.
        MapReadGet<ItemAnomalyScan>(
            app,
            ApiForensics + "/item-anomalies",
            (HttpContext ctx, ItemAnomalyReads reads, CancellationToken ct) =>
                OkAsync(reads.ScanAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.AuditRead,
            TagForensics
        );
        MapReadGet<ModerationStats>(
            app,
            ApiForensics + "/moderation/stats",
            (HttpContext ctx, AuditReads reads, CancellationToken ct) =>
                OkAsync(reads.ModerationStatsAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.AuditRead,
            TagForensics
        );
    }
}
