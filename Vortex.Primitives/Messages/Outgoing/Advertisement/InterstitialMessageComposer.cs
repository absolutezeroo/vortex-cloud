using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Advertisement;

[GenerateSerializer, Immutable]
public sealed record InterstitialMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
