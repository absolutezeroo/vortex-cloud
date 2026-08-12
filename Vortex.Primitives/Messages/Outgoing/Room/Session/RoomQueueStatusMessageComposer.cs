using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Room.Session;

/// <summary>
/// Where the player stands in a room's entry queues (header 530).
///
/// Shape from WIN63's parser (unknowns/_SafePkg_1744/_SafeCls_3217.as): a flat id, a count of queue
/// sets, and for each set a name, a target, and its own counted list of queues. The client takes
/// the first set's target as the active one, so order is meaningful here - this is not an unordered
/// collection.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record RoomQueueStatusMessageComposer : IComposer
{
    [Id(0)]
    public required int FlatId { get; init; }

    /// <summary>The first entry becomes the client's active target.</summary>
    [Id(1)]
    public required ImmutableArray<RoomQueueSet> QueueSets { get; init; }
}

/// <summary>One set of queues, keyed by its target in the client's map.</summary>
[GenerateSerializer, Immutable]
public sealed record RoomQueueSet
{
    [Id(0)]
    public required string Name { get; init; }

    [Id(1)]
    public required int Target { get; init; }

    [Id(2)]
    public required ImmutableArray<RoomQueueEntry> Queues { get; init; }
}

/// <summary>A named queue and how many players are waiting in it.</summary>
[GenerateSerializer, Immutable]
public sealed record RoomQueueEntry
{
    [Id(0)]
    public required string Name { get; init; }

    [Id(1)]
    public required int Count { get; init; }
}
