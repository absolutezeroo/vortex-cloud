using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms;

namespace Vortex.Primitives.Messages.Outgoing.Navigator;

/// <summary>Ack sent after a successful EditEventMessage -- confirms the room advertisement's
/// updated ad copy so the client's "my events" panel can refresh without a full re-fetch.</summary>
[GenerateSerializer, Immutable]
public sealed record RoomEventMessageComposer : IComposer
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
    public required int MinutesRemaining { get; init; }
}
