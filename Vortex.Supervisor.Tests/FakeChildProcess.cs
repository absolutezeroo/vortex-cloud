using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Supervisor.Configuration;
using Vortex.Supervisor.Process;

namespace Vortex.Supervisor.Tests;

/// <summary>
///     A child process whose death and whose <see cref="Exited"/> notification are two separate,
///     independently triggerable events — which is the whole point. The real
///     <see cref="System.Diagnostics.Process"/> raises Exited on a thread pool thread some time after
///     the process is gone, and the gap between the two is where the interesting bugs live.
/// </summary>
internal sealed class FakeChildProcess(int id) : IChildProcess
{
    private readonly TaskCompletionSource _exited = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );

    public event EventHandler? Exited;

    public int Id { get; } = id;

    public bool HasExited { get; private set; }

    public int ExitCode { get; private set; }

    public bool WasKilled { get; private set; }

    public bool WasDisposed { get; private set; }

    public List<string> Input { get; } = [];

    /// <summary>Whether writing the graceful shutdown command actually ends this process.</summary>
    public bool ObeysShutdown { get; set; } = true;

    public string ShutdownCommand { get; set; } = "quit";

    public Action<string>? Output { get; private set; }

    public void Start(Action<string> onOutput) => Output = onOutput;

    public Task WriteLineAsync(string line, CancellationToken ct)
    {
        Input.Add(line);

        if (ObeysShutdown && line == ShutdownCommand)
        {
            ExitSilently();
        }

        return Task.CompletedTask;
    }

    public Task WaitForExitAsync(CancellationToken ct) => _exited.Task.WaitAsync(ct);

    public void Kill()
    {
        WasKilled = true;
        ExitSilently(137);
    }

    public void Dispose() => WasDisposed = true;

    /// <summary>Ends the process without telling anyone — the notification comes separately.</summary>
    public void ExitSilently(int code = 0)
    {
        HasExited = true;
        ExitCode = code;
        _exited.TrySetResult();
    }

    /// <summary>Delivers the exit notification, whenever the test decides it arrives.</summary>
    public void AnnounceExit() => Exited?.Invoke(this, EventArgs.Empty);
}

internal sealed class FakeChildProcessFactory : IChildProcessFactory
{
    private int _nextId = 1000;

    public List<FakeChildProcess> Created { get; } = [];

    public IChildProcess Create(EmulatorProcessConfig config, string workingDirectory)
    {
        FakeChildProcess process = new(++_nextId)
        {
            ShutdownCommand = config.GracefulShutdownCommand,
        };

        Created.Add(process);

        return process;
    }
}
