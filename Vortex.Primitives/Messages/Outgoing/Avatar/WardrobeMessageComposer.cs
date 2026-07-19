using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Avatar;

[GenerateSerializer, Immutable]
public sealed record WardrobeMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
