using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Habbicons;

/// <summary>
/// Somebody in the room used a Habbicon. Sent to everyone in it, including the user, so the
/// speaker's own client draws it from the server's copy rather than optimistically.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record RoomUseHabbiconMessageComposer : IComposer
{
    /// <summary>The user's room object id -- the index the client draws avatars by, not a player id.</summary>
    [Id(0)]
    public required int RoomIndex { get; init; }

    [Id(1)]
    public required int HabbiconId { get; init; }
}
