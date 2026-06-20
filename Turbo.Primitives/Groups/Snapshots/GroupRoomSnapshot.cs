using Orleans;

namespace Turbo.Primitives.Groups.Snapshots;

/// <summary>A room the player owns, as offered in the guild creation/edit room picker.</summary>
[GenerateSerializer, Immutable]
public sealed record GroupRoomSnapshot
{
    [Id(0)]
    public required int RoomId { get; init; }

    [Id(1)]
    public required string RoomName { get; init; }

    [Id(2)]
    public required bool HasControllers { get; init; }
}
