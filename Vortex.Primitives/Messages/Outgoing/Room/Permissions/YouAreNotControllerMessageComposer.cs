using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Room.Permissions;

[GenerateSerializer, Immutable]
public sealed record YouAreNotControllerMessageComposer : IComposer
{
    /// <summary>The room the player has no rights in. The client reads it to know which room the
    /// answer is about.</summary>
    [Id(0)]
    public required int RoomId { get; init; }
}
