using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Room.Engine;

/// <summary>Dropping a bot from the inventory onto a tile. (0, 0) means "anywhere that works" —
/// the client sends it when the drop has no tile under it yet.</summary>
public record PlaceBotMessage : IMessageEvent
{
    public required int BotId { get; init; }
    public required int X { get; init; }
    public required int Y { get; init; }
}
