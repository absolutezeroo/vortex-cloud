using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Supervisor.Configuration;

namespace Vortex.Supervisor.Process;

/// <summary>The real thing: <see cref="IChildProcess"/> over <see cref="System.Diagnostics.Process"/>.</summary>
public sealed class SystemChildProcess : IChildProcess
{
    private readonly System.Diagnostics.Process _process;

    public SystemChildProcess(EmulatorProcessConfig config, string workingDirectory)
    {
        _process = new System.Diagnostics.Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = config.ExecutablePath,
                Arguments = config.Arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true,
            },
            EnableRaisingEvents = true,
        };

        _process.Exited += (_, _) => Exited?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? Exited;

    public int Id => _process.Id;

    public bool HasExited => _process.HasExited;

    public int ExitCode => _process.ExitCode;

    public void Start(Action<string> onOutput)
    {
        _process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                onOutput(e.Data);
            }
        };

        _process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                onOutput(e.Data);
            }
        };

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
    }

    public async Task WriteLineAsync(string line, CancellationToken ct)
    {
        await _process.StandardInput.WriteLineAsync(line.AsMemory(), ct).ConfigureAwait(false);
        await _process.StandardInput.FlushAsync(ct).ConfigureAwait(false);
    }

    public Task WaitForExitAsync(CancellationToken ct) => _process.WaitForExitAsync(ct);

    public void Kill() => _process.Kill(entireProcessTree: true);

    public void Dispose() => _process.Dispose();
}

public sealed class SystemChildProcessFactory : IChildProcessFactory
{
    public IChildProcess Create(EmulatorProcessConfig config, string workingDirectory) =>
        new SystemChildProcess(config, workingDirectory);
}
