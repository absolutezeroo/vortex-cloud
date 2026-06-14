using System.Collections.Generic;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Inventory.Badges;

public record SetActivatedBadgesMessage : IMessageEvent
{
    public required List<(int SlotId, string BadgeCode)> Slots { get; init; }
}
