using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Console;

namespace Vortex.Dashboard.API.Operations;

internal sealed partial class DashboardOperationsService
{
    /// <summary>
    ///     The operator commands, each flagged with whether <paramref name="holdsCapability"/> says
    ///     this caller may run it.
    /// </summary>
    public IReadOnlyList<ConsoleCommandInfo> ListConsoleCommands(Func<string, bool> holdsCapability)
    {
        List<ConsoleCommandInfo> commands = [];

        foreach (ConsoleCommandDescriptor descriptor in _consoleCommands.Commands)
        {
            commands.Add(
                new ConsoleCommandInfo(
                    descriptor.Name,
                    descriptor.Usage,
                    descriptor.Description,
                    descriptor.RequiredCapability,
                    descriptor.RequiredCapability is null
                        || holdsCapability(descriptor.RequiredCapability)
                )
            );
        }

        return commands;
    }

    public ConsoleCommandDescriptor? FindConsoleCommand(string command)
    {
        string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return parts.Length == 0 ? null : _consoleCommands.Find(parts[0]);
    }

    /// <summary>
    ///     Runs one operator command under the caller's identity, recording the line that was typed
    ///     and what it printed. The capability check happens at the endpoint, where the caller's
    ///     claims live; this is the audited execution half.
    /// </summary>
    public async Task<RunConsoleCommandResponse> RunConsoleCommandAsync(
        RunConsoleCommandRequest request,
        string actor,
        CancellationToken ct
    )
    {
        List<string> output = [];

        OperationResult result = await ExecuteAsync(
                "ops.console.run",
                actor,
                request.Reason,
                targetPlayerId: null,
                roomId: null,
                // The whole line, not just the verb: "mystery-box bob gold" and "mystery-box bob
                // rare" are different acts, and the audit is worthless if it cannot tell them apart.
                detail: new { command = request.Command },
                work: async c =>
                {
                    bool known = await _consoleCommands
                        .ExecuteAsync(request.Command, output.Add, c)
                        .ConfigureAwait(false);

                    if (!known)
                    {
                        throw new InvalidOperationException("unknown_command");
                    }
                },
                ct
            )
            .ConfigureAwait(false);

        return RunConsoleCommandResponse.From(result, output);
    }
}
