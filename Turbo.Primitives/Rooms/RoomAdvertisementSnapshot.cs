using System;
using Orleans;

namespace Turbo.Primitives.Rooms;

[GenerateSerializer, Immutable]
public sealed record RoomAdvertisementSnapshot
{
    [Id(0)]
    public required RoomId RoomId { get; init; }

    [Id(1)]
    public required string Name { get; init; }

    [Id(2)]
    public string? Description { get; init; }

    [Id(3)]
    public required int CategoryId { get; init; }

    [Id(4)]
    public required DateTime ExpiresAt { get; init; }
}
