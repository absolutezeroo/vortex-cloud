using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Inventory.Badges;

public record GetIsBadgeRequestFulfilledMessage : IMessageEvent
{
    public required string RequestCode { get; init; }
}
