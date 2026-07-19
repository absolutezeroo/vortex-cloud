using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Sound;

[GenerateSerializer, Immutable]
public sealed record NowPlayingMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
