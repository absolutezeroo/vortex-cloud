using Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;

namespace Vortex.Primitives.Rooms.RaidProtection;

/// <summary>
/// The answer to a save: what happened, and the room's state afterwards.
/// </summary>
/// <remarks>
/// The snapshot is not optional and is not the caller's draft. On anything but
/// <see cref="RaidProtectionSaveResult.Ok" /> the client redraws its whole panel from the settings
/// in the reply, so a failure that echoed the rejected draft back would leave the owner looking at
/// values the room does not hold. It is always the room's own state.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record RaidProtectionSaveOutcome
{
    [Id(0)]
    public required RaidProtectionSaveResult Result { get; init; }

    [Id(1)]
    public required RoomRaidProtectionSnapshot Settings { get; init; }
}
