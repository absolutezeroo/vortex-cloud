using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Sound;

[GenerateSerializer, Immutable]
public sealed record UserSongDisksInventoryMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
