using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Sound;

[GenerateSerializer, Immutable]
public sealed record PlayListMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
