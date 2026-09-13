using Orleans;

namespace Vortex.Primitives.Orleans.Snapshots.Room;

/// <summary>
/// The two mute facts the room card carries for one viewer: whether the room is silenced right now,
/// and whether this viewer is allowed to flip that switch. Both are live state, which is why they
/// are not on <see cref="RoomSnapshot"/> with the persisted settings.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record RoomMuteStateSnapshot
{
    [Id(0)]
    public required bool AllInRoomMuted { get; init; }

    [Id(1)]
    public required bool CanMute { get; init; }
}
