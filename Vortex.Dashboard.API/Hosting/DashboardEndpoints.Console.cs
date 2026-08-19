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
            "/api/ops/console/commands",
            (HttpContext ctx, DashboardOperationsService ops) =>
                Results.Ok(ops.ListConsoleCommands(ctx.HoldsCapability)),
            Capabilities.Dashboard.OpsServerConsole,
            TagConsole
        );
    }

    private static void MapConsoleOperations(WebApplication app)
    {
        MapPost(
            app,
            ApiOperations + "/console/run",
            "/api/ops/console/run",
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
