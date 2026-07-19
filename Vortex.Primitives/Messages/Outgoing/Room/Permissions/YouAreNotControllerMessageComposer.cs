using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Room.Permissions;

[GenerateSerializer, Immutable]
public sealed record YouAreNotControllerMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
