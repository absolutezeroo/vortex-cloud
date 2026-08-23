using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Camera;

[GenerateSerializer, Immutable]
public sealed record CameraStorageUrlMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
