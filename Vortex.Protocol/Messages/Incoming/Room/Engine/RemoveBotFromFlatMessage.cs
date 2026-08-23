using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Room.Engine;

public record RemoveBotFromFlatMessage : IMessageEvent
{
    public required int BotId { get; init; }
}
