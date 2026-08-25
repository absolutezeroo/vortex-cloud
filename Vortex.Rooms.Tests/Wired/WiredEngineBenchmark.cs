using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Action;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Events.Player;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Grains.Systems;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;
using Vortex.Rooms.Wired.Engine;
using Vortex.Tests.Support;
using Xunit;
using Xunit.Abstractions;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// The versioned wired benchmark the architecture note asks for (§10): empty room, loaded room, event
/// storm, and a chain actually firing.
/// </summary>
/// <remarks>
/// <para>
/// Off unless <c>VORTEX_BENCH=1</c>. A wall-clock measurement in the quality gate is a flake with a
/// schedule: it fails on a laptop that decided to index something and passes on a quiet afternoon,
/// and the second time it cries wolf everyone stops reading it. The complexity claims that <em>can</em>
/// hold everywhere live in <see cref="WiredEngineCostTests"/> and run every time.
/// </para>
/// <para>
/// This is not BenchmarkDotNet and does not pretend to be: one warm-up pass, a fixed iteration count,
/// and the mean. That is enough for what a baseline is actually for — noticing that a tick got twice
/// as expensive — and not enough to argue about five percent. Numbers only mean anything against
/// numbers from the same machine, which is why the report records what it ran on.
/// </para>
/// <para>
/// <c>VORTEX_BENCH=1 dotnet test Vortex.Rooms.Tests --filter WiredEngineBenchmark</c>, then commit
/// <c>docs/architecture-v4/benchmarks/wired-engine.md</c> if the machine is the reference one.
/// </para>
/// </remarks>
public sealed class WiredEngineBenchmark(ITestOutputHelper output)
{
    private const int WARMUP = 200;
    private const int ITERATIONS = 2_000;

    [Fact]
    public async Task MeasureAndWriteTheBaseline()
    {
        if (Environment.GetEnvironmentVariable("VORTEX_BENCH") != "1")
        {
            output.WriteLine("VORTEX_BENCH is not 1 — skipping. See the class summary.");

            return;
        }

        // Discarded. Whichever scenario runs first pays the JIT for the whole engine, which showed
        // up as the empty room costing five times a room with a thousand items in it -- a number that
        // is not wrong so much as measuring the wrong thing.
        await TickCostAsync(items: 10, triggers: true, chain: true);

        List<(string Scenario, double MicrosecondsPerTick)> results =
        [
            ("empty room", await TickCostAsync(items: 0, triggers: false, chain: false)),
            ("loaded room, no wired (1k items)", await TickCostAsync(1_000, false, false)),
            ("loaded room, idle trigger (1k items)", await TickCostAsync(1_000, true, false)),
            ("chain firing every tick", await TickCostAsync(50, true, true)),
            ("event storm (2k events per tick)", await StormCostAsync()),
        ];

        string report = Report(results);

        output.WriteLine(report);

        Write(report);
    }

    /// <summary>Mean microseconds for one <c>ProcessWiredAsync</c> on a settled room.</summary>
    private static async Task<double> TickCostAsync(int items, bool triggers, bool chain)
    {
        (FakeWiredRoomHost room, RoomWiredSystem engine) = Build(items, triggers, chain);

        long now = 1_000;

        for (int i = 0; i < WARMUP; i++)
        {
            await engine.ProcessWiredAsync(now += 50, CancellationToken.None);
        }

        long startedAt = Stopwatch.GetTimestamp();

        for (int i = 0; i < ITERATIONS; i++)
        {
            await engine.ProcessWiredAsync(now += 50, CancellationToken.None);
        }

        return Stopwatch.GetElapsedTime(startedAt).TotalMicroseconds / ITERATIONS;
    }

    /// <summary>
    /// Mean microseconds for a tick that also had to absorb two thousand raised events. The queue cap
    /// bounds what a tick consumes, so this measures the acceptance path as much as the drain.
    /// </summary>
    private static async Task<double> StormCostAsync()
    {
        (FakeWiredRoomHost room, RoomWiredSystem engine) = Build(200, triggers: true, chain: true);

        long now = 1_000;
        const int TICKS = 100;

        for (int i = 0; i < 10; i++)
        {
            await engine.ProcessWiredAsync(now += 50, CancellationToken.None);
        }

        long startedAt = Stopwatch.GetTimestamp();

        for (int tick = 0; tick < TICKS; tick++)
        {
            for (int i = 0; i < 2_000; i++)
            {
                await engine.OnRoomEventAsync(PlayerLeft(room), CancellationToken.None);
            }

            await engine.ProcessWiredAsync(now += 50, CancellationToken.None);
        }

        return Stopwatch.GetElapsedTime(startedAt).TotalMicroseconds / TICKS;
    }

    private static (FakeWiredRoomHost Room, RoomWiredSystem Engine) Build(
        int items,
        bool triggers,
        bool chain
    )
    {
        FakeWiredRoomHost room = new();

        for (int objectId = 1; objectId <= items; objectId++)
        {
            room.With(WiredTestBoxes.FloorItem(objectId, logic: null!), tileIdx: objectId % 50);
        }

        if (triggers)
        {
            room.With(
                WiredTestBoxes.FloorItem(
                    items + 1,
                    chain ? new FiringTrigger(items + 1) : new SilentTrigger(items + 1)
                ),
                tileIdx: 0
            );
        }

        if (chain)
        {
            room.With(WiredTestBoxes.FloorItem(items + 2, new NoopAction(items + 2)), tileIdx: 0);
        }

        return (room, new RoomWiredSystem(room));
    }

    private static PlayerLeftEvent PlayerLeft(FakeWiredRoomHost room) =>
        new()
        {
            RoomId = room.RoomId,
            CausedBy = ActionContext.CreateForWired(room.RoomId),
            PlayerId = new PlayerId(1),
        };

    private static string Report(List<(string Scenario, double MicrosecondsPerTick)> results)
    {
        StringBuilder sb = new();

        sb.AppendLine("# Wired engine baseline");
        sb.AppendLine();
        sb.AppendLine(
            "Mean microseconds per `ProcessWiredAsync`, over "
                + ITERATIONS.ToString("N0", CultureInfo.InvariantCulture)
                + " iterations after "
                + WARMUP.ToString("N0", CultureInfo.InvariantCulture)
                + " warm-up ticks."
        );
        sb.AppendLine();
        sb.AppendLine(
            "Only comparable against numbers from the same machine. Regenerate with "
                + "`VORTEX_BENCH=1 dotnet test Vortex.Rooms.Tests --filter WiredEngineBenchmark`."
        );
        sb.AppendLine();
        sb.AppendLine("| Scenario | µs / tick |");
        sb.AppendLine("|---|---:|");

        foreach ((string scenario, double micros) in results)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"| {scenario} | {micros:F2} |");
        }

        sb.AppendLine();
        sb.AppendLine(
            "A room ticks every `RoomTickMs` (50 ms = 50,000 µs), so the last column against that "
                + "number is the share of one room's budget the wired step spends."
        );
        sb.AppendLine();
        sb.AppendLine(
            "Measured against `FakeWiredRoomHost`, so these are the engine's own costs: the "
                + "orchestration, the index, the scheduler, the pile resolution. What a real room "
                + "spends looking items up in its own state is not in here."
        );
        sb.AppendLine();
        sb.AppendLine("## Measured on");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"- {Environment.OSVersion.VersionString}");
        sb.AppendLine(
            CultureInfo.InvariantCulture,
            $"- {Environment.ProcessorCount} logical processors"
        );
        sb.AppendLine(CultureInfo.InvariantCulture, $"- .NET {Environment.Version}");

        return sb.ToString();
    }

    /// <summary>
    /// Writes beside the architecture notes when the repository is findable from the test's own
    /// directory, and says nothing if it is not — a benchmark that cannot file its report is still a
    /// benchmark, and the numbers are in the test output either way.
    /// </summary>
    private void Write(string report)
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);

        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "docs")))
        {
            dir = dir.Parent;
        }

        if (dir is null)
        {
            output.WriteLine("No docs/ directory above the test binary — report not written.");

            return;
        }

        string target = Path.Combine(dir.FullName, "docs", "architecture-v4", "benchmarks");

        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "wired-engine.md"), report);

        output.WriteLine($"Wrote {Path.Combine(target, "wired-engine.md")}");
    }

    /// <summary>Indexed, listens for the event the storm raises, and never fires.</summary>
    private sealed class SilentTrigger(int objectId)
        : FurnitureWiredTriggerLogic(
            FakeProxy.Create<IGrainFactory>(_ => null),
            new StuffDataFactory(),
            WiredTestBoxes.Context(objectId)
        )
    {
        public override int WiredCode => 0;

        public override List<Type> SupportedEventTypes { get; } = [];

        protected override Task FillInternalDataAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FiringTrigger(int objectId)
        : FurnitureWiredTriggerLogic(
            FakeProxy.Create<IGrainFactory>(_ => null),
            new StuffDataFactory(),
            WiredTestBoxes.Context(objectId, 0)
        )
    {
        public override int WiredCode => 0;

        public override List<Type> SupportedEventTypes { get; } = [typeof(PlayerLeftEvent)];

        public override Task<bool> CanTriggerAsync(
            IWiredProcessingContext ctx,
            CancellationToken ct
        ) => Task.FromResult(true);

        protected override Task FillInternalDataAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class NoopAction(int objectId)
        : FurnitureWiredActionLogic(
            FakeProxy.Create<IGrainFactory>(_ => null),
            new StuffDataFactory(),
            WiredTestBoxes.Context(objectId, 0)
        )
    {
        public override int WiredCode => 0;

        public override int GetDelayMs() => 0;

        public override Task<bool> ExecuteAsync(IWiredExecutionContext ctx, CancellationToken ct) =>
            Task.FromResult(true);

        protected override Task FillInternalDataAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
