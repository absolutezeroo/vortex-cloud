using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Inventory.Trading;

[GenerateSerializer, Immutable]
public sealed record TradeSilverFeeMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
