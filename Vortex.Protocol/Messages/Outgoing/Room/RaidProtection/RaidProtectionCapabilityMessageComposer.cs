using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Room.RaidProtection;

/// <summary>
/// Tells one player whether they may manage this room's raid protection.
/// </summary>
/// <remarks>
/// Not optional, and not only about a button. In AIR 1.0.31 <c>canManage()</c> gates the settings
/// packet and the save reply as well as the room-info button, so a player who never receives this
/// has the whole feature silently disabled — including answers to messages they just sent. It goes
/// out on room entry, to every visitor, <c>canManage</c> false included.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record RaidProtectionCapabilityMessageComposer : IComposer
{
    [Id(0)]
    public required int RoomId { get; init; }

    [Id(1)]
    public required bool CanManage { get; init; }
}
