using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Snapshots.Avatars;

namespace Vortex.Primitives.Messages.Outgoing.Room.Engine;

[GenerateSerializer, Immutable]
public sealed record UserUpdateMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<RoomAvatarSnapshot> Avatars { get; init; }
}
