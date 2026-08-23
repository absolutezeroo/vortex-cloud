using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Gifts;

[GenerateSerializer, Immutable]
public sealed record PhoneCollectionStateMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
