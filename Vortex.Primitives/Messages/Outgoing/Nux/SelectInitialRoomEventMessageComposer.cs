using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Nux;

[GenerateSerializer, Immutable]
public sealed record SelectInitialRoomEventMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
