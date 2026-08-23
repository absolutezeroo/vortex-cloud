using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Inventory.Trading;

[GenerateSerializer, Immutable]
public sealed record TradingNotOpenEventMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
