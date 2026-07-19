using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Inventory.Pets;

[GenerateSerializer, Immutable]
public sealed record PetReceivedMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
