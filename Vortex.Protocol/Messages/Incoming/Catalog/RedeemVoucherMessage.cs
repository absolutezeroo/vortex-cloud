using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Catalog;

public record RedeemVoucherMessage : IMessageEvent
{
    public string? Code { get; init; }
}
