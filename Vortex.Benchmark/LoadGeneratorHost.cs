using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vortex.Primitives.Benchmark;

namespace Vortex.Benchmark;

/// <summary>
/// Runs the load generator as a separate process and reads its samples back.
/// </summary>
/// <remarks>
/// <para>
/// The generator used to live in here, and it quietly ruined its own measurements: a hundred
/// synthetic players meant two hundred loops waking ten times a second inside the process under
/// test. When a run showed multi-second stalls there was no honest way to say whose they were.
/// </para>
/// <para>
/// Now the emulator does what only it can — the accounts, the room, the furniture, all of which need
/// the database — and hands the generator a list of tickets and a port. What comes back is one JSON
/// line per second on stdout. The only thing the two share is the wire, which is the point.
/// </para>
/// </remarks>
internal sealed class LoadGeneratorHost(ILogger<LoadGeneratorHost> logger)
{
    private static readonly JsonSerializerOptions Wire = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// The generator sits beside this assembly: it is referenced by the host project purely so the
    /// build drops it here, never so that a line of it is loaded in this process.
    /// </summary>
    public static string ExecutablePath =>
        Path.Combine(
            AppContext.BaseDirectory,
            OperatingSystem.IsWindows() ? "Vortex.LoadGen.exe" : "Vortex.LoadGen"
        );

    /// <summary>
    /// The managed assembly the launcher above is only a shim for. Checked separately because a
    /// build once copied the shim and left this behind, and the failure that produced —
    /// "The application to execute does not exist" on the child's stderr, after a hundred accounts
    /// had already been created — said nothing about which of the two was missing.
    /// </summary>
    private static string AssemblyPath =>
        Path.Combine(AppContext.BaseDirectory, "Vortex.LoadGen.dll");

    public static bool IsAvailable => File.Exists(ExecutablePath) && File.Exists(AssemblyPath);

    /// <summary>
    /// Runs one load, reporting each second as it arrives. Returns when the generator exits, which
    /// it does on its own when the duration is up, or when <paramref name="ct"/> kills it.
    /// </summary>
    public async Task RunAsync(
        LoadGeneratorPlan plan,
        Action<BenchmarkSample> onSample,
        CancellationToken ct
    )
    {
        if (!IsAvailable)
        {
            // Refused here, before the accounts and the furniture exist. A run that provisions and
            // then discovers it has nothing to run with leaves the teardown to clean up a hotel that
            // was never loaded.
            logger.LogError(
                "Load generator is not deployed: {Executable} exists={ExeFound}, {Assembly} exists={DllFound}",
                ExecutablePath,
                File.Exists(ExecutablePath),
                AssemblyPath,
                File.Exists(AssemblyPath)
            );

            throw new InvalidOperationException("benchmark_generator_missing");
        }

        ProcessStartInfo start = new()
        {
            FileName = ExecutablePath,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = AppContext.BaseDirectory,
        };

        using Process process =
            Process.Start(start)
            ?? throw new InvalidOperationException("benchmark_generator_start");

        try
        {
            await process
                .StandardInput.WriteAsync(JsonSerializer.Serialize(plan, Wire).AsMemory(), ct)
                .ConfigureAwait(false);

            // Closing stdin is the signal that the plan is complete: the generator reads to end.
            process.StandardInput.Close();

            Task errors = DrainErrorsAsync(process, ct);

            while (await process.StandardOutput.ReadLineAsync(ct).ConfigureAwait(false) is { } line)
            {
                if (Parse(line) is { } sample)
                {
                    onSample(sample);
                }
            }

            await process.WaitForExitAsync(ct).ConfigureAwait(false);
            await errors.ConfigureAwait(false);
        }
        finally
        {
            // A stopped run must not leave a process holding a hundred sockets open against the
            // hotel. Killing it is safe: everything it created lives in this process's database, and
            // the teardown that follows does not need its cooperation.
            if (!process.HasExited)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Load generator would not stop.");
                }
            }
        }
    }

    private async Task DrainErrorsAsync(Process process, CancellationToken ct)
    {
        // Read rather than ignored: a full stderr pipe blocks the child, and a generator that
        // failed to connect explains itself here and nowhere else.
        while (await process.StandardError.ReadLineAsync(ct).ConfigureAwait(false) is { } line)
        {
            logger.LogWarning("Load generator: {Line}", line);
        }
    }

    internal static BenchmarkSample? Parse(string line)
    {
        try
        {
            LoadGeneratorSample? sample = JsonSerializer.Deserialize<LoadGeneratorSample>(
                line,
                Wire
            );

            return sample is null
                ? null
                : new BenchmarkSample
                {
                    AtUtc = DateTime.UtcNow,
                    ConnectedClients = sample.Connected,
                    RttMedianMs = sample.RttMedianMs,
                    RttP95Ms = sample.RttP95Ms,
                    PacketsReceived = sample.Packets,
                    BytesReceived = sample.Bytes,
                    Failures = sample.Failures,
                };
        }
        catch (JsonException)
        {
            // A line that is not a sample is not fatal -- it is the generator writing something we
            // did not ask for, and dropping it beats ending the run over it.
            return null;
        }
    }
}

internal sealed record LoadGeneratorPlan
{
    public required string Host { get; init; }
    public required int Port { get; init; }
    public required int RoomId { get; init; }
    public required int DurationSeconds { get; init; }
    public required int RampSeconds { get; init; }
    public required int WalkIntervalMs { get; init; }
    public required int ChatIntervalMs { get; init; }
    public required ImmutableArray<string> Tickets { get; init; }
    public required int[][] WalkTargets { get; init; }
}

internal sealed record LoadGeneratorSample
{
    public int Connected { get; init; }
    public double RttMedianMs { get; init; }
    public double RttP95Ms { get; init; }
    public long Packets { get; init; }
    public long Bytes { get; init; }
    public long Failures { get; init; }
}
