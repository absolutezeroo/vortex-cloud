using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Game.Directory;

[GenerateSerializer, Immutable]
public sealed record Game2UserJoinedGameMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
