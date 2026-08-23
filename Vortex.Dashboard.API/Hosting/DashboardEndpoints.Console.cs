using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Vortex.Dashboard.API.Operations;
using Vortex.Dashboard.API.Security;
using Vortex.Primitives.Console;
using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Hosting;

internal static partial class DashboardEndpoints
{
    private const string TagConsole = "Console";

    private static void MapConsoleReads(WebApplication app)
    {
        MapReadGet(
            app,
            ApiOperations + "/console/commands",
            (HttpContext ctx, DashboardOperationsService ops) =>
                Results.Ok(ops.ListConsoleCommands(ctx.HoldsCapability)),
            Capabilities.Dashboard.OpsServerConsole,
            TagConsole
        );

        // Server-sent events rather than a socket: the payload is one-way, line-oriented text, and
        // EventSource reconnects on its own across a dashboard restart. Mapped directly instead of
        // through MapReadGet because the response is a stream, not a JSON document.
        app.MapGet(ApiOperations + "/console/stream", StreamConsoleAsync)
            .RequireAuthorization(Capabilities.Dashboard.ServerConsoleRead)
            .WithTags(TagConsole);
    }

    /// <summary>
    ///     Replays the buffered console, then follows it live until the viewer goes away.
    /// </summary>
    private static async Task StreamConsoleAsync(HttpContext ctx, ServerConsoleFeed feed)
    {
        ctx.Response.Headers.ContentType = "text/event-stream";
        ctx.Response.Headers.CacheControl = "no-cache";
        // Without this a reverse proxy will hold the stream in a buffer and deliver it in chunks,
        // which turns a live console into a stuttering one.
        ctx.Response.Headers["X-Accel-Buffering"] = "no";

        using ServerConsoleSubscription subscription = feed.Subscribe();

        foreach (string line in subscription.Backlog)
        {
            await WriteEventAsync(ctx, line).ConfigureAwait(false);
        }

        await ctx.Response.Body.FlushAsync(ctx.RequestAborted).ConfigureAwait(false);

        try
        {
            await foreach (
                string line in subscription
                    .Reader.ReadAllAsync(ctx.RequestAborted)
                    .ConfigureAwait(false)
            )
            {
                await WriteEventAsync(ctx, line).ConfigureAwait(false);
                await ctx.Response.Body.FlushAsync(ctx.RequestAborted).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // The operator navigated away; the subscription detaches on the way out.
        }
    }

    private static async Task WriteEventAsync(HttpContext ctx, string line) =>
        await ctx
            .Response.WriteAsync($"data: {JsonSerializer.Serialize(line)}\n\n", ctx.RequestAborted)
            .ConfigureAwait(false);

    private static void MapConsoleOperations(WebApplication app)
    {
        MapPost(
            app,
            ApiOperations + "/console/run",
            async (
                HttpContext ctx,
                RunConsoleCommandRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                ConsoleCommandDescriptor? descriptor = ops.FindConsoleCommand(body.Command);

                if (descriptor is null)
                {
                    return Results.BadRequest(new { error = "unknown_command" });
                }

                // The group's capability only says "may use the console at all". Each command also
                // carries the capability of whatever it acts on, so console access never becomes a
                // side door around the page that normally gates that action.
                if (
                    descriptor.RequiredCapability is not null
                    && !ctx.HoldsCapability(descriptor.RequiredCapability)
                )
                {
                    return Results.Forbid();
                }

                return Results.Ok(
                    await ops.RunConsoleCommandAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsServerConsole,
            TagConsole
        );
    }
}
