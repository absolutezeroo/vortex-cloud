using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Moderation;

/// <summary>
/// Answer to the mod tool's room-tool panel. The trailing room block is optional on the wire: when
/// <see cref="RoomExists"/> is false the client stops reading, which is how a moderator is told the
/// room disappeared between the report and the lookup.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record ModeratorRoomInfoEventMessageComposer : IComposer
{
    [Id(0)]
    public required int RoomId { get; init; }

    [Id(1)]
    public required int UserCount { get; init; }

    [Id(2)]
    public required bool OwnerInRoom { get; init; }

    [Id(3)]
    public required int OwnerId { get; init; }

    [Id(4)]
    public required string OwnerName { get; init; }

    [Id(5)]
    public required bool RoomExists { get; init; }

    [Id(6)]
    public string RoomName { get; init; } = string.Empty;

    [Id(7)]
    public string RoomDescription { get; init; } = string.Empty;

    [Id(8)]
    public ImmutableArray<string> Tags { get; init; } = ImmutableArray<string>.Empty;
}
