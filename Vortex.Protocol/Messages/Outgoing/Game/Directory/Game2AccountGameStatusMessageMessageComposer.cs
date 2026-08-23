using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Game.Directory;

[GenerateSerializer, Immutable]
public sealed record Game2AccountGameStatusMessageMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
