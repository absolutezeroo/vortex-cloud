using System;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Console;

namespace Vortex.Main.Console;

/// <summary>
/// The stdin front-end onto <see cref="IConsoleCommandDispatcher"/>: reads lines from the operator's
/// terminal and prints whatever the command writes. The commands themselves live in the dispatcher
/// so the dashboard console reaches the same set.
/// </summary>
public class ConsoleCommandService(IConsoleCommandDispatcher dispatcher)
{
    private readonly IConsoleCommandDispatcher _dispatcher = dispatcher;
    private readonly CancellationTokenSource _cts = new();

    private Task? _loopTask;

    public bool IsRunning => _loopTask is { IsCompleted: false };

    public void Enable()
    {
        System.Console.WriteLine("Console command service started. Type 'help' for commands.");

        if (IsRunning)
        {
            throw new InvalidOperationException("Already running.");
        }

        _loopTask = Task.Run(() => LoopAsync(_cts.Token));
    }

    public async Task DisableAsync()
    {
        if (!IsRunning)
        {
            return;
        }

        await _cts.CancelAsync().ConfigureAwait(false);

        if (_loopTask is not null)
#pragma warning disable VSTHRD003
        {
            await _loopTask.ConfigureAwait(false);
        }
#pragma warning restore VSTHRD003

        _cts.Dispose();
    }

    private async Task LoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            string? input = await Task.Run(System.Console.ReadLine, ct).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            await _dispatcher
                .ExecuteAsync(input, System.Console.WriteLine, ct)
                .ConfigureAwait(false);
        }
    }
}
