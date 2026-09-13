using Orleans;

namespace Vortex.Primitives.Orleans.Snapshots.Room;

/// <summary>
/// One room's raid-protection state, in the shape the client reads it.
/// </summary>
/// <remarks>
/// The field order here is the wire order — <c>RaidProtectionSettingsSnapshot.readAfterRoomId</c>
/// in AIR 1.0.31 reads exactly these ten in exactly this sequence, and the same snapshot is what
/// both the settings packet and the save reply carry. Keeping one record for both is what stops the
/// reply from drifting out of step with the push.
/// <para>
/// The last two are not settings and the client never sends them back: they are what the room
/// reports about itself.
/// </para>
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record RoomRaidProtectionSnapshot
{
    [Id(0)]
    public required int RoomId { get; init; }

    [Id(1)]
    public required bool Enabled { get; init; }

    [Id(2)]
    public required int DetectionSensitivity { get; init; }

    [Id(3)]
    public required int ActionType { get; init; }

    [Id(4)]
    public required int BanDurationSeconds { get; init; }

    [Id(5)]
    public required bool GuardEnabled { get; init; }

    [Id(6)]
    public required int GuardDurationSeconds { get; init; }

    [Id(7)]
    public required int GuardSensitivity { get; init; }

    /// <summary>Whether a raid is being handled right now. Live state, never persisted.</summary>
    [Id(8)]
    public required bool IncidentActive { get; init; }

    /// <summary>Unix seconds of the last incident, or 0 if this room has never seen one.</summary>
    [Id(9)]
    public required int LastRaidAtEpochSeconds { get; init; }
}
