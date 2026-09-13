using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans.Snapshots.Room;

namespace Vortex.Protocol.Messages.Outgoing.Room.RaidProtection;

/// <summary>
/// The room's raid-protection state, in answer to the panel being opened.
/// </summary>
/// <remarks>
/// The client only acts on this for the room it is currently standing in and only when it holds the
/// capability, so an unsolicited push to a player elsewhere is dropped on the floor.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record RaidProtectionSettingsMessageComposer : IComposer
{
    [Id(0)]
    public required RoomRaidProtectionSnapshot Settings { get; init; }
}
