using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Inventory.Furni;

[GenerateSerializer, Immutable]
public sealed record FurniListInvalidateEventMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
