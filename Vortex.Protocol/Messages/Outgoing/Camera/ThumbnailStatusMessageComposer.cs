using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Camera;

[GenerateSerializer, Immutable]
public sealed record ThumbnailStatusMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
