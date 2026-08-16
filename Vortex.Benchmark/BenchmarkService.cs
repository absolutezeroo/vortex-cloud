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
        List<SyntheticClient> clients = [];
        BenchmarkFixture? fixture = null;
        ProcessCounters before = ProcessCounters.Capture();

        try
        {
            _state = _state with { Phase = BenchmarkPhase.Provisioning };

            fixture = await provisioner
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

            // Everything the run drives hangs off this, not off `ct` directly, so the duration can
            // end the run on its own. Awaiting the sampler on a token only the Stop button cancels
            // meant a run sat in Steady after its time was up, waiting for a human -- the duration
            // was a suggestion.
            using CancellationTokenSource runCts = CancellationTokenSource.CreateLinkedTokenSource(
                ct
            );

            Task sampler = Task.Run(
                () => SampleAsync(clients, runCts.Token),
                CancellationToken.None
            );

            await RampAsync(plan, fixture, host, port, clients, runCts.Token).ConfigureAwait(false);

            _state = _state with { Phase = BenchmarkPhase.Steady };

            await Task.Delay(TimeSpan.FromSeconds(plan.DurationSeconds), runCts.Token)
                .ConfigureAwait(false);

            await runCts.CancelAsync().ConfigureAwait(false);
            await sampler.ConfigureAwait(false);
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

            foreach (SyntheticClient client in clients)
            {
                client.Dispose();
            }

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

    private async Task RampAsync(
        BenchmarkPlan plan,
        BenchmarkFixture fixture,
        string host,
        int port,
        List<SyntheticClient> clients,
        CancellationToken ct
    )
    {
        // Spread the arrivals. All at once would measure the accept path under a thundering herd,
        // which is a real question but not this one -- and it would leave the steady phase measuring
        // a hotel still busy digesting the login storm.
        int gapMs =
            plan.RampSeconds > 0 && plan.Players > 1
                ? Math.Max(1, plan.RampSeconds * 1000 / plan.Players)
                : 0;

        for (int index = 0; index < fixture.Tickets.Length; index++)
        {
            ct.ThrowIfCancellationRequested();

            SyntheticClient client = new(host, port);

            try
            {
                await client.ConnectAsync(fixture.Tickets[index], ct).ConfigureAwait(false);

                clients.Add(client);

                _ = Task.Run(() => client.ReceiveLoopAsync(ct), CancellationToken.None);
                _ = Task.Run(() => DriveAsync(client, plan, fixture, index, ct), ct);

                _state = _state with { ConnectedClients = clients.Count };
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                client.Dispose();

                logger.LogDebug(ex, "Benchmark client {Index} could not connect.", index);
            }

            if (gapMs > 0)
            {
                await Task.Delay(gapMs, ct).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// What one synthetic player does for a living: enter the room, then walk, talk and ping on
    /// their own timers.
    /// </summary>
    private static async Task DriveAsync(
        SyntheticClient client,
        BenchmarkPlan plan,
        BenchmarkFixture fixture,
        int seed,
        CancellationToken ct
    )
    {
        try
        {
            // The login has to land before the room will have them, and the answer that would say so
            // is one of the composers this client deliberately cannot read. A fixed pause is the
            // honest trade: measured from the steady phase, not the ramp, so it costs nothing.
            await Task.Delay(500, ct).ConfigureAwait(false);
            await client.EnterRoomAsync(fixture.RoomId, ct).ConfigureAwait(false);
            await Task.Delay(500, ct).ConfigureAwait(false);

            // Each action keeps its own clock rather than counting one-second ticks. The tick counter
            // it replaces could only ever express whole seconds -- "every 500 ms" quietly became
            // every second, and "every 2500 ms" became every two -- so the field on the page did not
            // mean what it said.
            long now = Environment.TickCount64;
            long nextWalk = now;
            long nextChat = now;
            long nextPing = now;
            int step = seed;

            while (!ct.IsCancellationRequested)
            {
                now = Environment.TickCount64;

                if (now >= nextPing)
                {
                    await client.PingAsync(ct).ConfigureAwait(false);

                    nextPing = now + 1000;
                }

                if (plan.WalkIntervalMs > 0 && now >= nextWalk)
                {
                    // A tile the room really has, taken from its own map. Walking at a hole is
                    // refused before the pathfinder runs, which is the half worth measuring -- and
                    // the offset by seed stops four hundred avatars from all heading for one tile,
                    // which is a queue, not a load.
                    (int x, int y) = fixture.WalkTargets[
                        Math.Abs(step + seed) % fixture.WalkTargets.Length
                    ];

                    await client.WalkAsync(x, y, ct).ConfigureAwait(false);

                    step++;
                    nextWalk = now + plan.WalkIntervalMs;
                }

                if (plan.ChatIntervalMs > 0 && now >= nextChat)
                {
                    await client
                        .SayAsync(string.Create(CultureInfo.InvariantCulture, $"bench {step}"), ct)
                        .ConfigureAwait(false);

                    nextChat = now + plan.ChatIntervalMs;
                }

                // Fine enough to honour a half-second interval without spinning.
                await Task.Delay(100, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // The run ended.
        }
    }

    private async Task SampleAsync(List<SyntheticClient> clients, CancellationToken ct)
    {
        using PeriodicTimer timer = new(TimeSpan.FromSeconds(1));

        try
        {
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                SyntheticClient[] snapshot = [.. clients];
                List<double> roundTrips = [];

                foreach (SyntheticClient client in snapshot)
                {
                    while (client.RoundTrips.TryDequeue(out long ticks))
                    {
                        roundTrips.Add(ticks * 1000.0 / Stopwatch.Frequency);
                    }
                }

                roundTrips.Sort();

                _samples.Enqueue(
                    new BenchmarkSample
                    {
                        AtUtc = DateTime.UtcNow,
                        ConnectedClients = snapshot.Count(client => client.Connected),
                        RttMedianMs = Percentile(roundTrips, 0.50),
                        RttP95Ms = Percentile(roundTrips, 0.95),
                        PacketsReceived = snapshot.Sum(client =>
                            Interlocked.Read(ref client.PacketsReceived)
                        ),
                        BytesReceived = snapshot.Sum(client =>
                            Interlocked.Read(ref client.BytesReceived)
                        ),
                        Failures = snapshot.Sum(client => Interlocked.Read(ref client.Failures)),
                    }
                );
            }
        }
        catch (OperationCanceledException)
        {
            // The run ended.
        }
    }

    /// <summary>
    /// Exact rather than bucketed: a run's sample is a few thousand round trips at most, and sorting
    /// them once a second costs less than the error a histogram would introduce at the tail — which
    /// is the end of the distribution anyone actually asks about.
    /// </summary>
    private static double Percentile(List<double> sorted, double percentile)
    {
        if (sorted.Count == 0)
        {
            return 0;
        }

        int index = (int)Math.Ceiling(percentile * sorted.Count) - 1;

        return sorted[Math.Clamp(index, 0, sorted.Count - 1)];
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
