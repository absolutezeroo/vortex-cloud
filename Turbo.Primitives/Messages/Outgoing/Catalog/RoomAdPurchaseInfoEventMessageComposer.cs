using System.Collections.Immutable;
using Orleans;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Catalog;

[GenerateSerializer, Immutable]
public sealed record RoomAdPurchaseInfoEventMessageComposer : IComposer
{
    [Id(0)]
    public required bool IsVip { get; init; }

    [Id(1)]
    public required ImmutableArray<RoomAdRoomEntry> Rooms { get; init; }
}

[GenerateSerializer, Immutable]
public sealed record RoomAdRoomEntry
{
    [Id(0)]
    public required int RoomId { get; init; }

    [Id(1)]
    public required string RoomName { get; init; }

    /// <summary>No event-room concept exists yet -- always false.</summary>
    [Id(2)]
    public required bool IsEventRoom { get; init; }
}
