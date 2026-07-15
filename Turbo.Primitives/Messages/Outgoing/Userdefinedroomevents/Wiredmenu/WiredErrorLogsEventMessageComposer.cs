using System.Collections.Generic;
using Orleans;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;

[GenerateSerializer, Immutable]
public sealed record WiredErrorLogsEventMessageComposer : IComposer
{
    [Id(0)]
    public required List<WiredErrorLogEntry> Entries { get; init; }
}

[GenerateSerializer, Immutable]
public sealed record WiredErrorLogEntry
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
