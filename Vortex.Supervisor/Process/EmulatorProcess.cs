using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Console;
using Vortex.Supervisor.Configuration;

namespace Vortex.Supervisor.Process;

public enum EmulatorState
{
    Stopped,
    Starting,
    Running,
    Stopping,
}

/// <summary>
///     Owns the emulator process: starts it, stops it gracefully, restarts it, and pipes its console
///     both ways.
/// </summary>
public sealed class EmulatorProcess(
    IOptions<SupervisorConfig> config,
    IChildProcessFactory processFactory,
    ServerConsoleFeed console,
    ILogger<EmulatorProcess> logger
) : IDisposable
{
    private readonly EmulatorProcessConfig _config = config.Value.Emulator;

    /// <summary>Serialises whole operations (start, stop, restart) against each other.</summary>
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>
    ///     Protects the fields below against the exit callback, which arrives on a thread pool
    ///     thread and holds no gate — the semaphore orders operations, this orders field access.
    /// </summary>
    private readonly Lock _stateLock = new();

    private IChildProcess? _current;

    public EmulatorState State { get; private set; } = EmulatorState.Stopped;

    public int? ProcessId { get; private set; }

    public async Task StartAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);

        try
        {
            await StartCoreAsync().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task StopAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);

        try
        {
            await StopCoreAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    ///     Stops and starts under a single hold of the gate. Releasing between the two halves would
    ///     let a concurrent start, stop or restart interleave and leave two emulators running (or
    ///     none).
    /// </summary>
    public async Task RestartAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);

        try
        {
            await StopCoreAsync(ct).ConfigureAwait(false);
            await StartCoreAsync().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    ///     Turns the configured working directory into an absolute path, anchored on the supervisor's
    ///     own directory rather than the current one.
    ///     <para>
    ///     <see cref="Path.GetFullPath(string)"/> alone resolves against the process's current
    ///     directory, which is wherever the operator happened to be standing when they started it —
    ///     so the same configuration pointed at a different emulator depending on whether the
    ///     supervisor was launched from the repository root or from its own project folder. Anchoring
    ///     on the assembly's directory makes the configured path mean one thing.
    ///     </para>
    /// </summary>
    public static string ResolveWorkingDirectory(string configured) =>
        // Path.Combine returns the second argument untouched when it is already rooted, so an
        // absolute configured path still wins.
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configured));

    /// <summary>Writes one line to the emulator's stdin, where its console command service reads it.</summary>
    public async Task<bool> SendInputAsync(string line, CancellationToken ct)
    {
        IChildProcess? process;

        lock (_stateLock)
        {
            process = _current;
        }

        if (process is null || State != EmulatorState.Running)
        {
            console.Publish("[supervisor] Cannot send input: the emulator is not running.");

            return false;
        }

        try
        {
            await process.WriteLineAsync(line, ct).ConfigureAwait(false);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write to the emulator's stdin.");
            console.Publish($"[supervisor] Failed to send input: {ex.Message}");

            return false;
        }
    }

    private async Task StartCoreAsync()
    {
        if (State is EmulatorState.Running or EmulatorState.Starting)
        {
            console.Publish("[supervisor] The emulator is already running.");

            return;
        }

        State = EmulatorState.Starting;

        string workingDirectory = ResolveWorkingDirectory(_config.WorkingDirectory);

        console.Publish(
            $"[supervisor] Starting {_config.ExecutablePath} {_config.Arguments} in {workingDirectory}"
        );

        if (!Directory.Exists(workingDirectory))
        {
            logger.LogError(
                "Emulator working directory does not exist: {WorkingDirectory}",
                workingDirectory
            );
            console.Publish($"[supervisor] Working directory not found: {workingDirectory}");
            State = EmulatorState.Stopped;

            return;
        }

        IChildProcess process = processFactory.Create(_config, workingDirectory);

        process.Exited += (_, _) => OnExited(process);

        try
        {
            process.Start(console.Publish);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start the emulator process.");
            console.Publish($"[supervisor] Failed to start: {ex.Message}");
            process.Dispose();
            State = EmulatorState.Stopped;

            return;
        }

        lock (_stateLock)
        {
            _current = process;
            ProcessId = process.Id;
            State = EmulatorState.Running;
        }

        console.Publish($"[supervisor] Emulator started (pid {process.Id}).");

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private async Task StopCoreAsync(CancellationToken ct)
    {
        IChildProcess? process;

        lock (_stateLock)
        {
            process = _current;
        }

        if (process is null || State is EmulatorState.Stopped or EmulatorState.Stopping)
        {
            console.Publish("[supervisor] The emulator is not running.");

            return;
        }

        State = EmulatorState.Stopping;
        console.Publish("[supervisor] Asking the emulator to shut down…");

        try
        {
            if (!process.HasExited)
            {
                await process
                    .WriteLineAsync(_config.GracefulShutdownCommand, ct)
                    .ConfigureAwait(false);

                using CancellationTokenSource timeout = new(
                    TimeSpan.FromSeconds(_config.GracefulShutdownTimeoutSeconds)
                );
                using CancellationTokenSource linked =
                    CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token);

                try
                {
                    await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (timeout.IsCancellationRequested)
                {
                    console.Publish(
                        "[supervisor] Graceful shutdown timed out; killing the process."
                    );
                    process.Kill();
                    await process.WaitForExitAsync(ct).ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while stopping the emulator process.");
            console.Publish($"[supervisor] Error while stopping: {ex.Message}");
        }

        // Claim the slot here rather than leaving it to the exit handler: by the time this returns
        // the caller (a restart, say) is entitled to start a new process, and a stale _current would
        // make the next stop target a corpse.
        lock (_stateLock)
        {
            if (ReferenceEquals(_current, process))
            {
                _current = null;
                ProcessId = null;
                State = EmulatorState.Stopped;
            }
        }

        process.Dispose();
    }

    private void OnExited(IChildProcess process)
    {
        int? exitCode = null;

        try
        {
            exitCode = process.ExitCode;
        }
        catch (InvalidOperationException)
        {
            // The process object was already disposed by a concurrent stop; the code is gone with it.
        }

        console.Publish(
            exitCode is null
                ? "[supervisor] The emulator exited."
                : $"[supervisor] The emulator exited (code {exitCode})."
        );

        // Exited fires on a thread pool thread and can arrive long after a restart has already
        // launched the replacement. Reporting the exit is always right; claiming the slot is only
        // right if this is still the process occupying it — otherwise a late notification marks the
        // running emulator as stopped and drops its handle, leaving a live hotel the panel believes
        // is down and can no longer stop, restart or talk to.
        lock (_stateLock)
        {
            if (ReferenceEquals(_current, process))
            {
                _current = null;
                ProcessId = null;
                State = EmulatorState.Stopped;
            }
        }
    }

    public void Dispose()
    {
        _current?.Dispose();
        _gate.Dispose();
    }
}
