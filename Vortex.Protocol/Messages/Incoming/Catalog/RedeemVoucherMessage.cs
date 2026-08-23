using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Catalog;

public record RedeemVoucherMessage : IMessageEvent
{
    public string? Code { get; init; }
}
