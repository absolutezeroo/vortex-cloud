using System.Collections.Generic;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Users;

public record CreateGuildMessage : IMessageEvent
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required int PrimaryColorId { get; init; }
    public required int SecondaryColorId { get; init; }
    public required int BaseRoomId { get; init; }

    /// <summary>Flattened badge parts (groups of three ints: partId, colorId, position).</summary>
    public required IReadOnlyList<int> BadgeParts { get; init; }
}
