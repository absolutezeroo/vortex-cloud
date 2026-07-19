using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Inventory.Badges;

public record RequestABadgeMessage : IMessageEvent
{
    public required string RequestCode { get; init; }
}
