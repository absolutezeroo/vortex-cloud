using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Room.Engine;

[GenerateSerializer, Immutable]
public sealed record FavoriteMembershipUpdateMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
