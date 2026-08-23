using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Competition;

[GenerateSerializer, Immutable]
public sealed record SecondsUntilMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
