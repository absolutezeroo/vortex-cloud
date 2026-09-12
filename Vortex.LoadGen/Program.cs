using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Vortex.LoadGen;

/// <summary>
/// The load generator, as its own process.
/// </summary>
/// <remarks>
/// <para>
/// It used to run inside the emulator, and that made every number it produced suspect: a hundred
/// synthetic players meant a hundred receive loops and a hundred drive loops waking ten times a
/// second, roughly a thousand thread-pool wakeups that no real hotel would ever have — inside the
/// very process being measured. When the run showed multi-second stalls there was no way to say
/// whether they belonged to the hotel or to the tool.
/// </para>
/// <para>
/// So the tool moved out. The emulator still does the provisioning, because it owns the database;
/// this reads a plan on stdin, opens the sockets from outside, and writes one JSON line per second
/// to stdout. Nothing is shared but the wire.
/// </para>
/// </remarks>
internal static class Program
{
    /// <summary>
    /// The wire between the two processes. Public so a test can serialize a sample exactly as this
    /// writes one and hand it to the reader on the other side — that seam has no compiler to keep it
    /// honest, and a renamed field would simply yield a run with no samples and no error.
    /// </summary>
    internal static readonly JsonSerializerOptions Wire = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static async Task<int> Main()
    {
        string? input = await Console.In.ReadToEndAsync().ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(input))
        {
            await Console.Error.WriteLineAsync("no plan on stdin").ConfigureAwait(false);

            return 2;
        }

        LoadPlan? plan = JsonSerializer.Deserialize<LoadPlan>(input, Wire);

        if (plan is null || plan.Tickets.Length == 0)
        {
            await Console.Error.WriteLineAsync("empty plan").ConfigureAwait(false);

            return 2;
        }

        // The parent kills this process to stop a run early; the duration is the ordinary way out.
        using CancellationTokenSource cts = new();

        Console.CancelKeyPress += (_, args) =>
        {
            args.Cancel = true;
            cts.Cancel();
        };

        try
        {
            await RunAsync(plan, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Asked to stop.
        }

        return 0;
    }

    private static async Task RunAsync(LoadPlan plan, CancellationToken ct)
    {
        List<SyntheticClient> clients = [];

        try
        {
            Task sampler = Task.Run(() => SampleAsync(clients, ct), CancellationToken.None);

            await RampAsync(plan, clients, ct).ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(plan.DurationSeconds), ct).ConfigureAwait(false);
        }
        finally
        {
            foreach (SyntheticClient client in clients)
            {
                client.Dispose();
            }
        }
    }

    private static async Task RampAsync(
        LoadPlan plan,
        List<SyntheticClient> clients,
        CancellationToken ct
    )
    {
        int gapMs =
            plan.RampSeconds > 0 && plan.Tickets.Length > 1
                ? Math.Max(1, plan.RampSeconds * 1000 / plan.Tickets.Length)
                : 0;

        for (int index = 0; index < plan.Tickets.Length; index++)
        {
            ct.ThrowIfCancellationRequested();

            SyntheticClient client = new(plan.Host, plan.Port);

            try
            {
                await client.ConnectAsync(plan.Tickets[index], ct).ConfigureAwait(false);

                clients.Add(client);

                _ = Task.Run(() => client.ReceiveLoopAsync(ct), CancellationToken.None);
                _ = Task.Run(() => DriveAsync(client, plan, index, ct), CancellationToken.None);
            }
            catch (Exception) when (!ct.IsCancellationRequested)
            {
                client.Dispose();
            }

            if (gapMs > 0)
            {
                await Task.Delay(gapMs, ct).ConfigureAwait(false);
            }
        }
    }

    private static async Task DriveAsync(
        SyntheticClient client,
        LoadPlan plan,
        int seed,
        CancellationToken ct
    )
    {
        try
        {
            // Spread by index, not at random: a deterministic layout means two runs of the same plan
            // put the same crowd in the same places, which is what makes their numbers comparable.
            RoomPlan[] rooms =
                plan.Rooms.Length > 0 ? plan.Rooms : [new RoomPlan { RoomId = plan.RoomId }];

            int roomIndex = seed % rooms.Length;

            await Task.Delay(500, ct).ConfigureAwait(false);
            await client.EnterRoomAsync(rooms[roomIndex].RoomId, ct).ConfigureAwait(false);
            await Task.Delay(500, ct).ConfigureAwait(false);

            long start = Environment.TickCount64;

            // Every timer is offset by this client's index. Without it a hundred and fifty bots
            // would buy on the same millisecond and then sit idle for eight seconds, and the report
            // would show periodic stalls that the generator had manufactured itself.
            long nextWalk = start + Stagger(seed, plan.WalkIntervalMs);
            long nextChat = start + Stagger(seed, plan.ChatIntervalMs);
            long nextMove = start + Stagger(seed, plan.MoveIntervalMs);
            long nextUse = start + Stagger(seed, plan.UseIntervalMs);
            long nextBuy = start + Stagger(seed, plan.BuyIntervalMs);
            long nextMessage = start + Stagger(seed, plan.MessageIntervalMs);
            long nextRoom = start + Stagger(seed, plan.CreateRoomIntervalMs);
            long nextSwitch = start + Stagger(seed, plan.RoomSwitchIntervalMs);
            long nextPing = start;
            int step = seed;

            while (!ct.IsCancellationRequested)
            {
                long now = Environment.TickCount64;

                if (now >= nextPing)
                {
                    await client.PingAsync(ct).ConfigureAwait(false);

                    nextPing = now + 1000;
                }

                if (plan.WalkIntervalMs > 0 && now >= nextWalk && plan.WalkTargets.Length > 0)
                {
                    int[] target = plan.WalkTargets[Math.Abs(step) % plan.WalkTargets.Length];

                    await client.WalkAsync(target[0], target[1], ct).ConfigureAwait(false);

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

                // Dragging an item: position validation, restacking and a broadcast to everyone in
                // the room. Only ever an item this run placed — the fixture carries no other id.
                if (
                    plan.MoveIntervalMs > 0
                    && now >= nextMove
                    && rooms[roomIndex].FurnitureIds.Length > 0
                    && plan.WalkTargets.Length > 0
                )
                {
                    int[] here = rooms[roomIndex].FurnitureIds;
                    int objectId = here[Math.Abs(step) % here.Length];
                    int[] tile = plan.WalkTargets[Math.Abs(step + seed) % plan.WalkTargets.Length];

                    await client
                        .MoveFurnitureAsync(objectId, tile[0], tile[1], step % 8, ct)
                        .ConfigureAwait(false);

                    nextMove = now + plan.MoveIntervalMs;
                }

                if (
                    plan.UseIntervalMs > 0
                    && now >= nextUse
                    && rooms[roomIndex].FurnitureIds.Length > 0
                )
                {
                    int[] here = rooms[roomIndex].FurnitureIds;

                    await client
                        .UseFurnitureAsync(here[Math.Abs(step * 7) % here.Length], 0, ct)
                        .ConfigureAwait(false);

                    nextUse = now + plan.UseIntervalMs;
                }

                // Moving on. A real population does not stand still for five minutes, and the walk
                // from room to room is what exercises grain activation and deactivation — the very
                // path the periodic stalls were suspected to live on, and which a run where nobody
                // ever leaves can never touch.
                if (plan.RoomSwitchIntervalMs > 0 && now >= nextSwitch && rooms.Length > 1)
                {
                    roomIndex = (roomIndex + 1) % rooms.Length;

                    await client.EnterRoomAsync(rooms[roomIndex].RoomId, ct).ConfigureAwait(false);

                    nextSwitch = now + plan.RoomSwitchIntervalMs;
                }

                if (plan.BuyIntervalMs > 0 && now >= nextBuy && plan.CatalogOffers.Length > 0)
                {
                    int[] offer = plan.CatalogOffers[Math.Abs(step) % plan.CatalogOffers.Length];

                    await client.BuyAsync(offer[0], offer[1], ct).ConfigureAwait(false);

                    nextBuy = now + plan.BuyIntervalMs;
                }

                // The provisioner wires the accounts into a ring, so the player one index along is
                // this one's friend. Writing to anyone else would measure the refusal path.
                if (plan.MessageIntervalMs > 0 && now >= nextMessage && plan.PlayerIds.Length > 1)
                {
                    int friend = plan.PlayerIds[(seed + 1) % plan.PlayerIds.Length];

                    await client
                        .MessageAsync(
                            friend,
                            string.Create(CultureInfo.InvariantCulture, $"bench im {step}"),
                            ct
                        )
                        .ConfigureAwait(false);

                    nextMessage = now + plan.MessageIntervalMs;
                }

                // Named from the same marker the provisioner uses. Teardown finds these by owner
                // rather than by name, so the prefix is for whoever reads the room list, not safety.
                if (
                    plan.CreateRoomIntervalMs > 0
                    && now >= nextRoom
                    && plan.RoomModelName.Length > 0
                )
                {
                    await client
                        .CreateRoomAsync(
                            string.Create(CultureInfo.InvariantCulture, $"__bench__{seed}_{step}"),
                            plan.RoomModelName,
                            ct
                        )
                        .ConfigureAwait(false);

                    nextRoom = now + plan.CreateRoomIntervalMs;
                }

                await Task.Delay(100, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // The run ended.
        }
    }

    /// <summary>
    /// A stable per-client offset inside one interval, so the fleet spreads its attempts instead of
    /// firing them together. A multiplicative hash rather than <c>seed % interval</c>: consecutive
    /// indices must not land on consecutive milliseconds, or a ramp that connects them in order
    /// simply re-synchronises what this is meant to scatter.
    /// </summary>
    private static long Stagger(int seed, int intervalMs) =>
        intervalMs > 0 ? Math.Abs(seed * 2654435761L) % intervalMs : 0;

    private static async Task SampleAsync(List<SyntheticClient> clients, CancellationToken ct)
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

                LoadSample sample = new()
                {
                    Connected = snapshot.Count(client => client.Connected),
                    RttMedianMs = Percentile(roundTrips, 0.50),
                    RttP95Ms = Percentile(roundTrips, 0.95),
                    Packets = snapshot.Sum(client => Interlocked.Read(ref client.PacketsReceived)),
                    Bytes = snapshot.Sum(client => Interlocked.Read(ref client.BytesReceived)),
                    Failures = snapshot.Sum(client => Interlocked.Read(ref client.Failures)),
                };

                // One line, flushed. The parent reads these as they come rather than waiting for the
                // process to end, which is what keeps the dashboard's graph live.
                await Console
                    .Out.WriteLineAsync(JsonSerializer.Serialize(sample, Wire).AsMemory(), ct)
                    .ConfigureAwait(false);
                await Console.Out.FlushAsync(ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // The run ended.
        }
    }

    private static double Percentile(List<double> sorted, double percentile)
    {
        if (sorted.Count == 0)
        {
            return 0;
        }

        int index = (int)Math.Ceiling(percentile * sorted.Count) - 1;

        return sorted[Math.Clamp(index, 0, sorted.Count - 1)];
    }
}

/// <summary>Everything this process needs, and nothing about the hotel it is pointed at.</summary>
internal sealed record LoadPlan
{
    public string Host { get; init; } = "127.0.0.1";
    public int Port { get; init; } = 40000;
    public int RoomId { get; init; }
    public int DurationSeconds { get; init; } = 60;
    public int RampSeconds { get; init; } = 10;
    public int WalkIntervalMs { get; init; } = 2000;
    public int ChatIntervalMs { get; init; } = 8000;
    public string[] Tickets { get; init; } = [];
    public int[][] WalkTargets { get; init; } = [];

    // Everything below is the hotel-shaped half of the load. A zero interval leaves the behaviour
    // out, and an empty array does the same: the drive loop checks both, so a plan from an older
    // emulator still runs as a walk-and-chat plan rather than crashing on a missing field.
    public int MoveIntervalMs { get; init; }
    public int UseIntervalMs { get; init; }
    public int BuyIntervalMs { get; init; }
    public int MessageIntervalMs { get; init; }
    public int CreateRoomIntervalMs { get; init; }
    public RoomPlan[] Rooms { get; init; } = [];
    public int RoomSwitchIntervalMs { get; init; }
    public int[] PlayerIds { get; init; } = [];
    public int[][] CatalogOffers { get; init; } = [];
    public string RoomModelName { get; init; } = string.Empty;
}

/// <summary>
/// One room a bot may stand in, and the furniture it may touch while it is there.
/// </summary>
/// <remarks>
/// A room is one Orleans grain, and a grain answers one call at a time. Which room a bot picks is
/// therefore the single biggest lever on what a run measures: all of them in one room measures that
/// room's ceiling, spread over many measures the hotel.
/// </remarks>
internal sealed record RoomPlan
{
    public int RoomId { get; init; }
    public int[] FurnitureIds { get; init; } = [];
}

public sealed record LoadSample
{
    public required int Connected { get; init; }
    public required double RttMedianMs { get; init; }
    public required double RttP95Ms { get; init; }
    public required long Packets { get; init; }
    public required long Bytes { get; init; }
    public required long Failures { get; init; }
}
