using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Catalog;

[GenerateSerializer, Immutable]
public sealed record VoucherRedeemErrorMessageComposer : IComposer
{
    /// <summary>Localisation key the client shows, for example "voucher_redeem_error". Empty is
    /// tolerated; omitting it is not, as the client reads the string either way.</summary>
    [Id(0)]
    public string ErrorCode { get; init; } = string.Empty;
}
