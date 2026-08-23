using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Catalog;

[GenerateSerializer, Immutable]
public sealed record VoucherRedeemOkMessageComposer : IComposer
{
    /// <summary>What the voucher granted, shown in the client's confirmation dialog. Both are read
    /// unconditionally by the client, so both are always written.</summary>
    [Id(0)]
    public string ProductDescription { get; init; } = string.Empty;

    [Id(1)]
    public string ProductName { get; init; } = string.Empty;
}
