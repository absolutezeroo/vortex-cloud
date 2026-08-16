using System;
using System.Collections.Immutable;

namespace Vortex.Primitives.Benchmark;

/// <summary>
/// What a run should do to the hotel: how many players arrive, how much furniture they find, and for
/// how long.
/// </summary>
/// <remarks>
/// <para>
/// <paramref name="RampSeconds"/> is not decoration. Opening every connection at once measures how
/// the accept path copes with a thundering herd, which is a different question from how the hotel
/// copes with the players once they are in — and the second is the one being asked here. Arrivals
/// are spread over the ramp so the steady phase is steady.
/// </para>
/// <para>
/// The intervals decide what each synthetic player costs. A walk is the expensive one: it wakes the
/// room's pathfinder and every avatar in the room hears about the result, so the load a run applies
/// grows with the <em>square</em> of the player count, not with it.
/// </para>
/// </remarks>
public sealed record BenchmarkPlan
{
    public required int Players { get; init; }

    public required int Furniture { get; init; }

    /// <summary>
    /// The room to load, or 0 for a throwaway one built for the run.
    /// <para>
    /// Naming a real room is the more useful measurement and the more dangerous one: it is <em>your</em>
    /// room, with its own furniture and its own wired, being asked to carry the load — which is the
    /// question anyone actually has. Furniture the run adds to it is stamped so the cleanup can tell
    /// its own items from yours, and nothing that was there before is touched.
    /// </para>
    /// </summary>
    public int RoomId { get; init; }

    /// <summary>
    /// Which furniture to place, by definition id. Empty picks one plain floor item automatically.
    /// <para>
    /// Several is closer to a real room than one repeated: the client loads a sprite per definition,
    /// so a room of one item flatters it in a way no real room would. They are spread over the
    /// floor together rather than in blocks, for the same reason.
    /// </para>
    /// </summary>
    public ImmutableArray<int> FurnitureDefinitionIds { get; init; } = [];

    public required int DurationSeconds { get; init; }

    public int RampSeconds { get; init; } = 10;

    public int WalkIntervalMs { get; init; } = 2000;

    public int ChatIntervalMs { get; init; } = 8000;

    /// <summary>Free text kept with the result, so a run can be told from the one before it.</summary>
    public string Label { get; init; } = string.Empty;
}

/// <summary>Where a run has got to.</summary>
public enum BenchmarkPhase
{
    /// <summary>Nothing has ever run, or the last result has been cleared.</summary>
    Idle,

    /// <summary>Creating the room, the accounts and the furniture.</summary>
    Provisioning,

    /// <summary>Players are arriving.</summary>
    Ramping,

    /// <summary>Everyone is in and the measurement is the one that counts.</summary>
    Steady,

    /// <summary>Putting the hotel back as it was.</summary>
    TearingDown,

    Finished,

    Failed,
}

/// <summary>One second of a run, as it looked from both ends.</summary>
/// <remarks>
/// The round trip is measured with <c>LatencyPingRequest</c>, which is the same probe the real
/// client uses — so a synthetic player's number and a real player's number mean the same thing and
/// can be read on one axis.
/// </remarks>
public sealed record BenchmarkSample
{
    public required DateTime AtUtc { get; init; }

    public required int ConnectedClients { get; init; }

    public required double RttMedianMs { get; init; }

    public required double RttP95Ms { get; init; }

    public required long PacketsReceived { get; init; }

    public required long BytesReceived { get; init; }

    /// <summary>Sends the server refused or dropped. Non-zero here invalidates the latency figures
    /// above: they are the round trips that <em>completed</em>.</summary>
    public required long Failures { get; init; }
}

/// <summary>The live state of the one run that may exist at a time.</summary>
public sealed record BenchmarkStatus
{
    public required BenchmarkPhase Phase { get; init; }

    public required BenchmarkPlan? Plan { get; init; }

    public required DateTime? StartedAtUtc { get; init; }

    public required DateTime? EndedAtUtc { get; init; }

    public required int ConnectedClients { get; init; }

    public required int PlacedFurniture { get; init; }

    public required int RoomId { get; init; }

    public required string? Error { get; init; }

    public required ImmutableArray<BenchmarkSample> Samples { get; init; }

    /// <summary>What the run left behind, if the teardown could not finish. Non-empty is a bug
    /// report, not a status: it means rows are sitting in the hotel that nobody asked for.</summary>
    public required string? Residue { get; init; }

    /// <summary>Whether <c>benchmark.enabled</c> allows a run at all. False is the default, and it is
    /// the reason a start refuses — surfaced here so the page can say so before the button is
    /// pressed rather than after.</summary>
    public required bool Enabled { get; init; }

    /// <summary>True when the run was pointed at a room that already existed.</summary>
    public required bool BorrowedRoom { get; init; }

    /// <summary>
    /// Where the run's report was written, once it has finished. This is the artefact — the samples
    /// above are for looking at, the file is for keeping, comparing and sending to somebody.
    /// </summary>
    public required string? ReportPath { get; init; }
}

public sealed record BenchmarkStartResult(bool Started, string? ErrorCode);

/// <summary>One past run, as the history lists it.</summary>
public sealed record BenchmarkRunSummary
{
    public required string FileName { get; init; }
    public required string Path { get; init; }
    public required long SizeBytes { get; init; }
    public required DateTime WrittenAtUtc { get; init; }
    public required int Players { get; init; }
    public required int Furniture { get; init; }
    public required int DurationSeconds { get; init; }
    public required string Label { get; init; }
    public required string Phase { get; init; }
    public required int RoomId { get; init; }
    public required bool BorrowedRoom { get; init; }
    public required int PeakClients { get; init; }
    public required double WorstRttMs { get; init; }
    public required long Failures { get; init; }

    /// <summary>The stored verdict, so the list can be scanned for the bad runs without opening each
    /// one. Empty for a report written before verdicts existed.</summary>
    public required string Grade { get; init; }

    public required string Headline { get; init; }
}
