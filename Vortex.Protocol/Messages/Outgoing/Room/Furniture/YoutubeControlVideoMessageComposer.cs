using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Room.Furniture;

[GenerateSerializer, Immutable]
public sealed record YoutubeControlVideoMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
