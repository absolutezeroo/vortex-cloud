using System.Collections.Generic;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Users;

public record UpdateGuildBadgeMessage : IMessageEvent
{
    public required int GroupId { get; init; }

    /// <summary>Flattened badge parts (groups of three ints: partId, colorId, position).</summary>
    public required IReadOnlyList<int> BadgeParts { get; init; }
}
