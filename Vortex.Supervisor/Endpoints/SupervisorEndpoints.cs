using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vortex.Supervisor.Configuration;
using Vortex.Supervisor.Console;
using Vortex.Supervisor.Health;
using Vortex.Supervisor.Process;

namespace Vortex.Supervisor.Endpoints;

public static class SupervisorEndpoints
{
    public static void MapSupervisorEndpoints(this IEndpointRouteBuilder app)
    {
        // Exchanges the bearer secret for an HttpOnly cookie, so the console stream (EventSource,
        // which cannot set headers) authenticates without putting the secret in a URL where it would
        // land in access logs and browser history.
        app.MapPost(
            "/api/session",
            (HttpContext http, IOptions<SupervisorConfig> config) =>
            {
                if (
                    !SupervisorAuth.TokenMatches(
                        SupervisorAuth.ExtractToken(http.Request),
                        config.Value.Token
                    )
                )
                {
                    return Results.Unauthorized();
                }

                http.Response.Cookies.Append(
                    SupervisorAuth.CookieName,
                    config.Value.Token,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        SameSite = SameSiteMode.Strict,
                        Secure = http.Request.IsHttps,
                        Path = "/",
                    }
                );

                return Results.NoContent();
            }
        );

        RouteGroupBuilder group = app.MapGroup("/api").AddEndpointFilter(RequireTokenAsync);

        group.MapGet(
            "/status",
            (EmulatorProcess emulator, EmulatorHealthProbe health) =>
                Results.Ok(
                    new
                    {
                        state = emulator.State.ToString(),
                        pid = emulator.ProcessId,
                        health = health.LastStatus,
                        healthCheckedUtc = health.LastCheckedUtc,
                    }
                )
        );

        group.MapPost(
            "/start",
            async (EmulatorProcess emulator, CancellationToken ct) =>
            {
                await emulator.StartAsync(ct).ConfigureAwait(false);

                return Results.Ok(
                    new { state = emulator.State.ToString(), pid = emulator.ProcessId }
                );
            }
        );

        group.MapPost(
            "/stop",
            async (EmulatorProcess emulator, CancellationToken ct) =>
            {
                await emulator.StopAsync(ct).ConfigureAwait(false);

                return Results.Ok(
                    new { state = emulator.State.ToString(), pid = emulator.ProcessId }
                );
            }
        );

        group.MapPost(
            "/restart",
            async (EmulatorProcess emulator, CancellationToken ct) =>
            {
                await emulator.RestartAsync(ct).ConfigureAwait(false);

                return Results.Ok(
                    new { state = emulator.State.ToString(), pid = emulator.ProcessId }
                );
            }
        );

        group.MapPost(
            "/console",
            async (ConsoleInput input, EmulatorProcess emulator, CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(input.Line))
                {
                    return Results.BadRequest(new { error = "A command is required." });
                }

                bool sent = await emulator.SendInputAsync(input.Line, ct).ConfigureAwait(false);

                return sent
                    ? Results.NoContent()
                    : Results.BadRequest(new { error = "The emulator is not running." });
            }
        );

        group.MapGet("/console", StreamConsoleAsync);
    }

    /// <summary>
    ///     Server-sent events rather than a socket: the payload is one-way, line-oriented text, and
    ///     SSE reconnects on its own when the supervisor itself restarts.
    /// </summary>
    private static async Task StreamConsoleAsync(HttpContext http, ConsoleBuffer console)
    {
        http.Response.Headers.ContentType = "text/event-stream";
        http.Response.Headers.CacheControl = "no-cache";
        // Without this a reverse proxy will happily hold the stream in a buffer and deliver it in
        // chunks, which turns a live console into a stuttering one.
        http.Response.Headers["X-Accel-Buffering"] = "no";

        using ConsoleSubscription subscription = console.Subscribe();

        foreach (string line in subscription.Backlog)
        {
            await WriteEventAsync(http, line).ConfigureAwait(false);
        }

        await http.Response.Body.FlushAsync(http.RequestAborted).ConfigureAwait(false);

        try
        {
            await foreach (
                string line in subscription
                    .Reader.ReadAllAsync(http.RequestAborted)
                    .ConfigureAwait(false)
            )
            {
                await WriteEventAsync(http, line).ConfigureAwait(false);
                await http.Response.Body.FlushAsync(http.RequestAborted).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // The viewer navigated away; the subscription disposes on the way out.
        }
    }

    private static async Task WriteEventAsync(HttpContext http, string line) =>
        await http
            .Response.WriteAsync($"data: {JsonSerializer.Serialize(line)}\n\n", http.RequestAborted)
            .ConfigureAwait(false);

    private static async ValueTask<object?> RequireTokenAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next
    )
    {
        SupervisorConfig config = context
            .HttpContext.RequestServices.GetRequiredService<IOptions<SupervisorConfig>>()
            .Value;

        return SupervisorAuth.TokenMatches(
            SupervisorAuth.ExtractToken(context.HttpContext.Request),
            config.Token
        )
            ? await next(context).ConfigureAwait(false)
            : Results.Unauthorized();
    }

    private sealed record ConsoleInput(string Line);
}
