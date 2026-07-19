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
    public static void MapModerationOperations(WebApplication app)
    {
        MapPost(
            app,
            ApiOperations + "/players/kick",
            "/api/ops/player/kick",
            async (
                HttpContext ctx,
                KickPlayerRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (body is null || body.PlayerId <= 0 || !HasReason(body.Reason))
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.KickPlayerAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsKickPlayer,
            TagOperations
        );
        MapPost(
            app,
            ApiOperations + "/players/ban",
            "/api/ops/player/ban",
            async (
                HttpContext ctx,
                BanPlayerRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (
                    body is null
                    || body.PlayerId <= 0
                    || !HasReason(body.Reason)
                    || !HasValidDuration(body.Permanent, body.DurationSeconds)
                )
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.BanPlayerAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsBanAccount,
            TagOperations
        );
        MapPost(
            app,
            ApiOperations + "/players/unban",
            "/api/ops/player/unban",
            async (
                HttpContext ctx,
                UnbanPlayerRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (body is null || body.PlayerId <= 0 || !HasReason(body.Reason))
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.UnbanPlayerAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsBanAccount,
            TagOperations
        );
        MapPost(
            app,
            ApiOperations + "/players/mute",
            "/api/ops/player/mute",
            async (
                HttpContext ctx,
                MutePlayerRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (
                    body is null
                    || body.PlayerId <= 0
                    || body.DurationSeconds <= 0
                    || !HasReason(body.Reason)
                )
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.MutePlayerAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsMutePlayer,
            TagOperations
        );
        MapPost(
            app,
            ApiOperations + "/players/trading-lock",
            "/api/ops/player/trading-lock",
            async (
                HttpContext ctx,
                TradingLockRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (
                    body is null
                    || body.PlayerId <= 0
                    || !HasReason(body.Reason)
                    || !HasValidDuration(body.Permanent, body.DurationSeconds)
                )
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.TradingLockAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsTradingLock,
            TagOperations
        );
        MapPost(
            app,
            ApiOperations + "/players/trading-unlock",
            "/api/ops/player/trading-unlock",
            async (
                HttpContext ctx,
                TradingUnlockRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (body is null || body.PlayerId <= 0 || !HasReason(body.Reason))
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.TradingUnlockAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsTradingLock,
            TagOperations
        );
        MapReadGet(
            app,
            ApiOperations + "/cfh/queue",
            "/api/ops/cfh/queue",
            async (DashboardOperationsService ops, CancellationToken ct) =>
                Results.Ok(await ops.GetCfhQueueAsync(ct).ConfigureAwait(false)),
            Capabilities.Dashboard.OpsCfhManage,
            TagOperations
        );
        MapPost(
            app,
            ApiOperations + "/cfh/pick",
            "/api/ops/cfh/pick",
            async (
                HttpContext ctx,
                PickCfhTicketsRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (body is null || body.IssueIds is null || body.IssueIds.Length == 0)
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.PickCfhTicketsAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsCfhManage,
            TagOperations
        );
        MapPost(
            app,
            ApiOperations + "/cfh/close",
            "/api/ops/cfh/close",
            async (
                HttpContext ctx,
                CloseCfhTicketsRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (
                    body is null
                    || body.IssueIds is null
                    || body.IssueIds.Length == 0
                    || body.Reason is < 1 or > 3
                )
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.CloseCfhTicketsAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsCfhManage,
            TagOperations
        );
        MapPost(
            app,
            ApiOperations + "/cfh/release",
            "/api/ops/cfh/release",
            async (
                HttpContext ctx,
                ReleaseCfhTicketsRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (body is null || body.IssueIds is null || body.IssueIds.Length == 0)
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.ReleaseCfhTicketsAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsCfhManage,
            TagOperations
        );
    }
}
