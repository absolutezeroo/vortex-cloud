using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Inventory.Badges;

public record RequestABadgeMessage : IMessageEvent
{
    public required string RequestCode { get; init; }
}
