using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Room.Bots;

/// <summary>Configures one of a bot's skills. The data is the command's own encoding and opaque to
/// the server, which stores and returns it without interpreting it.</summary>
public record CommandBotMessage : IMessageEvent
{
    public required int BotId { get; init; }
    public required int CommandId { get; init; }
    public required string Data { get; init; }
}
