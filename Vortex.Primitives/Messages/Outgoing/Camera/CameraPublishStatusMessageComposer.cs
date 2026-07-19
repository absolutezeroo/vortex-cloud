using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Camera;

[GenerateSerializer, Immutable]
public sealed record CameraPublishStatusMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
