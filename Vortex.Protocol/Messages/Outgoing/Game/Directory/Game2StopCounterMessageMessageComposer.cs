using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Game.Directory;

[GenerateSerializer, Immutable]
public sealed record Game2StopCounterMessageMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
