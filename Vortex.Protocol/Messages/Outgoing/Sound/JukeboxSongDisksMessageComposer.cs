using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Sound;

[GenerateSerializer, Immutable]
public sealed record JukeboxSongDisksMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
