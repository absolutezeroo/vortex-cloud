using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Rooms.Enums.Wired;

namespace Vortex.Primitives.Rooms.Snapshots.Wired;

/// <summary>One counted wired error, as the wired menu's error panel lists them. The counter is
/// per-room in-memory state; <c>MsSinceLastOccurrence</c> is resolved against the clock at read
/// time rather than stored.</summary>
[GenerateSerializer, Immutable]
public sealed record WiredErrorLogSnapshot
{
    [Id(0)]
    public required int ErrorId { get; init; }

    [Id(1)]
    public required string ErrorName { get; init; }

    [Id(2)]
    public required string Category { get; init; }

    [Id(3)]
    public required int ThrowCount { get; init; }

    [Id(4)]
    public required long MsSinceLastOccurrence { get; init; }
}

/// <summary>One persisted wired log line.</summary>
[GenerateSerializer, Immutable]
public sealed record WiredRoomLogSnapshot
{
    [Id(0)]
    public required long Id { get; init; }

    [Id(1)]
    public required WiredLogLevel LogLevel { get; init; }

    [Id(2)]
    public required WiredLogSource LogSource { get; init; }

    [Id(3)]
    public required string Message { get; init; }

    [Id(4)]
    public required long Timestamp { get; init; }

    [Id(5)]
    public required string TimestampStr { get; init; }
}

/// <summary>
/// One page of a room's wired log, with the filters that produced it echoed back so the client can
/// tell which request a page answers. Returned by the grain in place of the composer it used to
/// build itself — see <see cref="WiredRoomStatsSnapshot"/> for why.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record WiredRoomLogPageSnapshot
{
    [Id(0)]
    public required int TotalEntries { get; init; }

    [Id(1)]
    public required int CurrentPage { get; init; }

    [Id(2)]
    public required int Amount { get; init; }

    [Id(3)]
    public required List<WiredRoomLogSnapshot> Entries { get; init; }

    [Id(4)]
    public WiredLogLevel? LogLevelFilter { get; init; }

    [Id(5)]
    public WiredLogSource? LogSourceFilter { get; init; }

    [Id(6)]
    public string? Query { get; init; }
}
