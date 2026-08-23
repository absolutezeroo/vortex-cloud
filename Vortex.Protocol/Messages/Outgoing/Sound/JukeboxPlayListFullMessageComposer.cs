using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Sound;

[GenerateSerializer, Immutable]
public sealed record JukeboxPlayListFullMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
