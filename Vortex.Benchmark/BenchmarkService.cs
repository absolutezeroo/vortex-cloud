using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Primitives.Benchmark;
using Vortex.Primitives.Orleans;

namespace Vortex.Benchmark;

/// <summary>
/// Runs one load test at a time and keeps its result until the next one.
/// </summary>
/// <remarks>
/// <para>
/// The run is a background task, not a request: a hundred clients arriving over a ten-second ramp
/// outlives any HTTP call, so the dashboard starts it, polls it, and reads the result when it is
/// there.
/// </para>
/// <para>
/// It also sweeps at startup. A run that was interrupted by a crash left accounts and a room behind,
/// and the next boot is the first moment anything is in a position to notice.
/// </para>
/// </remarks>
internal sealed class BenchmarkService(
    BenchmarkProvisioner provisioner,
    BenchmarkReportWriter reportWriter,
    LoadGeneratorHost generator,
    IGrainFactory grainFactory,
    IConfiguration configuration,
    ILogger<BenchmarkService> logger
) : IBenchmarkService, IHostedService
{
    private const string EnabledKey = "benchmark.enabled";

    private readonly SemaphoreSlim _startLock = new(1, 1);
    private readonly ConcurrentQueue<BenchmarkSample> _samples = new();

    private CancellationTokenSource? _runCts;
    private Task? _run;
    private volatile BenchmarkState _state = BenchmarkState.Empty;
    private bool _enabled;

    async Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        // Not the run -- the sweep. Anything the marker still names is debris from a run that did
        // not get to finish, and leaving it would let the next teardown report someone else's mess.
        string? residue = await provisioner.TeardownAsync(cancellationToken).ConfigureAwait(false);

        if (residue is not null)
        {
            logger.LogWarning("Benchmark startup sweep could not finish: {Residue}", residue);
        }
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken) =>
        StopRunAsync(cancellationToken);

    public async Task<BenchmarkStartResult> StartAsync(BenchmarkPlan plan, CancellationToken ct)
    {
        if (plan.Players <= 0 || plan.DurationSeconds <= 0 || plan.Furniture < 0)
        {
            return new BenchmarkStartResult(false, "invalid_plan");
        }

        bool enabled = await ReadEnabledAsync().ConfigureAwait(false);

        if (!enabled)
        {
            // Off unless somebody turned it on, and off again is one config write away. This opens
            // hundreds of connections and writes hundreds of rows; it is not something a hotel
            // should be one mis-click away from at any moment.
            return new BenchmarkStartResult(false, "benchmark_disabled");
        }

        await _startLock.WaitAsync(ct).ConfigureAwait(false);

        try
        {
            if (_run is { IsCompleted: false })
            {
                return new BenchmarkStartResult(false, "benchmark_already_running");
            }

            _samples.Clear();
            _state = BenchmarkState.Starting(plan);
            _runCts = new CancellationTokenSource();
            _run = Task.Run(() => RunAsync(plan, _runCts.Token), CancellationToken.None);

            return new BenchmarkStartResult(true, null);
        }
        finally
        {
            _startLock.Release();
        }
    }

    public Task StopAsync(CancellationToken ct) => StopRunAsync(ct);

    public ImmutableArray<BenchmarkRunSummary> ListRuns(int limit) => reportWriter.List(limit);

    public Task<string?> ReadRunAsync(string fileName, CancellationToken ct) =>
        reportWriter.ReadAsync(fileName, ct);

    public BenchmarkStatus GetStatus()
    {
        BenchmarkState state = _state;

        return new BenchmarkStatus
        {
            Phase = state.Phase,
            Plan = state.Plan,
            StartedAtUtc = state.StartedAtUtc,
            EndedAtUtc = state.EndedAtUtc,
            ConnectedClients = state.ConnectedClients,
            PlacedFurniture = state.PlacedFurniture,
            RoomId = state.RoomId,
            Error = state.Error,
            Residue = state.Residue,
            ReportPath = state.ReportPath,
            Enabled = Volatile.Read(ref _enabled),
            BorrowedRoom = state.BorrowedRoom,
            Samples = [.. _samples],
        };
    }

    /// <summary>
    /// Reads the switch and remembers the answer, so the status the page polls can report it without
    /// a grain call every second.
    /// </summary>
    public async Task<bool> ReadEnabledAsync()
    {
        bool enabled = await grainFactory
            .GetServerConfigGrain()
            .GetBoolAsync(EnabledKey, false)
            .ConfigureAwait(false);

        Volatile.Write(ref _enabled, enabled);

        return enabled;
    }

    private async Task StopRunAsync(CancellationToken ct)
    {
        CancellationTokenSource? cts = _runCts;
        Task? run = _run;

        if (cts is null || run is null)
        {
            return;
        }

        await cts.CancelAsync().ConfigureAwait(false);

        try
        {
            await run.WaitAsync(TimeSpan.FromSeconds(30), ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Benchmark did not stop cleanly.");
        }
    }

    private async Task RunAsync(BenchmarkPlan plan, CancellationToken ct)
    {
        ProcessCounters before = ProcessCounters.Capture();

        try
        {
            _state = _state with { Phase = BenchmarkPhase.Provisioning };

            BenchmarkFixture fixture = await provisioner
                .ProvisionAsync(
                    plan.Players,
                    plan.Furniture,
                    plan.RoomId,
                    plan.FurnitureDefinitionIds,
                    ct
                )
                .ConfigureAwait(false);

            _state = _state with
            {
                RoomId = fixture.RoomId,
                PlacedFurniture = fixture.PlacedFurniture,
                BorrowedRoom = fixture.Borrowed,
                Phase = BenchmarkPhase.Ramping,
            };

            (string host, int port) = ReadListener();

            // Everything from here happens in another process. This one only reads what comes back,
            // which is the whole reason the numbers can be believed: a hundred synthetic players no
            // longer means two hundred loops competing with the hotel they are supposed to measure.
            await generator
                .RunAsync(
                    new LoadGeneratorPlan
                    {
                        Host = host,
                        Port = port,
                        RoomId = fixture.RoomId,
                        DurationSeconds = plan.DurationSeconds,
                        RampSeconds = plan.RampSeconds,
                        WalkIntervalMs = plan.WalkIntervalMs,
                        ChatIntervalMs = plan.ChatIntervalMs,
                        Tickets = fixture.Tickets,
                        WalkTargets =
                        [
                            .. fixture.WalkTargets.Select(tile => new[] { tile.X, tile.Y }),
                        ],
                    },
                    OnSample,
                    ct
                )
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Stopped on purpose, or the duration elapsed against a cancelled token. Not a failure:
            // the teardown below is what matters and it runs either way.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Benchmark run failed.");

            _state = _state with { Phase = BenchmarkPhase.Failed, Error = ex.Message };
        }
        finally
        {
            _state = _state with { Phase = BenchmarkPhase.TearingDown };

            string? residue = await provisioner
                .TeardownAsync(CancellationToken.None)
                .ConfigureAwait(false);

            _state = _state with
            {
                Phase =
                    _state.Phase == BenchmarkPhase.Failed
                        ? BenchmarkPhase.Failed
                        : BenchmarkPhase.Finished,
                EndedAtUtc = DateTime.UtcNow,
                ConnectedClients = 0,
                Residue = residue,
            };

            // Written last, so it describes the run as it finished -- teardown included. A failed run
            // gets a report too: the interesting ones usually are the failures.
            string? report = await reportWriter
                .WriteAsync(plan, GetStatus(), before, CancellationToken.None)
                .ConfigureAwait(false);

            _state = _state with { ReportPath = report };
        }
    }

    /// <summary>
    /// One second, as the generator saw it. Kept here rather than in the other process because the
    /// page reads this list live and the report is written from it.
    /// </summary>
    private void OnSample(BenchmarkSample sample)
    {
        _samples.Enqueue(sample);

        _state = _state with
        {
            Phase =
                _state.Phase == BenchmarkPhase.Ramping && sample.ConnectedClients > 0
                    ? BenchmarkPhase.Steady
                    : _state.Phase,
            ConnectedClients = sample.ConnectedClients,
        };
    }

    private (string Host, int Port) ReadListener()
    {
        // The run connects to this process's own listener, so it reads the same configuration the
        // listener was built from rather than being told a port that could drift out of step.
        IConfigurationSection listener = configuration.GetSection(
            "serverOptions:TcpServer:listeners:0"
        );

        string host = listener["ip"] ?? "127.0.0.1";
        string? port = listener["port"];

        return (
            host == "0.0.0.0" ? "127.0.0.1" : host,
            int.TryParse(port, CultureInfo.InvariantCulture, out int parsed) ? parsed : 40000
        );
    }

    private sealed record BenchmarkState
    {
        public required BenchmarkPhase Phase { get; init; }
        public required BenchmarkPlan? Plan { get; init; }
        public required DateTime? StartedAtUtc { get; init; }
        public required DateTime? EndedAtUtc { get; init; }
        public required int ConnectedClients { get; init; }
        public required int PlacedFurniture { get; init; }
        public required int RoomId { get; init; }
        public required string? Error { get; init; }
        public required string? Residue { get; init; }
        public required bool BorrowedRoom { get; init; }
        public required string? ReportPath { get; init; }

        public static readonly BenchmarkState Empty = new()
        {
            Phase = BenchmarkPhase.Idle,
            Plan = null,
            StartedAtUtc = null,
            EndedAtUtc = null,
            ConnectedClients = 0,
            PlacedFurniture = 0,
            RoomId = 0,
            Error = null,
            Residue = null,
            BorrowedRoom = false,
            ReportPath = null,
        };

        public static BenchmarkState Starting(BenchmarkPlan plan) =>
            Empty with
            {
                Phase = BenchmarkPhase.Provisioning,
                Plan = plan,
                StartedAtUtc = DateTime.UtcNow,
            };
    }
}
