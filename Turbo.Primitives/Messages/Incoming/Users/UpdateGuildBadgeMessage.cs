using System.Collections.Generic;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Users;

public record UpdateGuildBadgeMessage : IMessageEvent
{
    public required int GroupId { get; init; }

    /// <summary>Flattened badge parts (groups of three ints: partId, colorId, position).</summary>
    public required IReadOnlyList<int> BadgeParts { get; init; }
}
