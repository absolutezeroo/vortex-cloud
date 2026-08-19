using System;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Supervisor.Configuration;

namespace Vortex.Supervisor.Process;

/// <summary>
///     The child process seen through the handful of operations the supervisor actually performs.
///     Exists so <see cref="EmulatorProcess"/>'s lifecycle logic — which is where the interesting
///     races live — can be exercised without spawning anything.
/// </summary>
public interface IChildProcess : IDisposable
{
    int Id { get; }

    bool HasExited { get; }

    int ExitCode { get; }

    /// <summary>Raised once, when the process ends, from an arbitrary thread.</summary>
    event EventHandler? Exited;

    /// <summary>Launches the process and begins pumping stdout and stderr into <paramref name="onOutput"/>.</summary>
    void Start(Action<string> onOutput);

    Task WriteLineAsync(string line, CancellationToken ct);

    Task WaitForExitAsync(CancellationToken ct);

    /// <summary>Ends the process and everything it spawned, without waiting.</summary>
    void Kill();
}

public interface IChildProcessFactory
{
    IChildProcess Create(EmulatorProcessConfig config, string workingDirectory);
}
