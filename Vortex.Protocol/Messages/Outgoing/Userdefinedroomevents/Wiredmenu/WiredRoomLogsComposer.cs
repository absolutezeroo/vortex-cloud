using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Enums.Wired;

namespace Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;

[GenerateSerializer, Immutable]
public sealed record WiredRoomLogsComposer : IComposer
{
    [Id(0)]
    public required int TotalEntries { get; init; }

    [Id(1)]
    public required int CurrentPage { get; init; }

    [Id(2)]
    public required int Amount { get; init; }

    [Id(3)]
    public required List<WiredRoomLogEntry> Entries { get; init; }

    [Id(4)]
    public WiredLogLevel? LogLevelFilter { get; init; }

    [Id(5)]
    public WiredLogSource? LogSourceFilter { get; init; }

    [Id(6)]
    public string? Query { get; init; }
}

[GenerateSerializer, Immutable]
public sealed record WiredRoomLogEntry
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
