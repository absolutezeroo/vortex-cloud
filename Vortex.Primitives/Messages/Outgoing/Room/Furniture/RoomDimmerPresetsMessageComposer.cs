using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Room.Furniture;

[GenerateSerializer, Immutable]
public sealed record RoomDimmerPresetsMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
