using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Inventory.Furni;

[GenerateSerializer, Immutable]
public sealed record FurniListInvalidateEventMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
