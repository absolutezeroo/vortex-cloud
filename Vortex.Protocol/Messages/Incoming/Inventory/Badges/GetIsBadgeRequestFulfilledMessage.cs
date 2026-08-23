using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Inventory.Badges;

public record GetIsBadgeRequestFulfilledMessage : IMessageEvent
{
    public required string RequestCode { get; init; }
}
