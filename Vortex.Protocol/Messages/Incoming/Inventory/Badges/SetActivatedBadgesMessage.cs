using System.Collections.Generic;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Inventory.Badges;

public record SetActivatedBadgesMessage : IMessageEvent
{
    public required List<(int SlotId, string BadgeCode)> Slots { get; init; }
}
