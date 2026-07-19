using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Catalog;

[GenerateSerializer, Immutable]
public sealed record VoucherRedeemErrorMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
