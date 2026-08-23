using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Room.Bots;

/// <summary>Opening a skill's configuration dialog: what is this bot currently set to?</summary>
public record GetBotCommandConfigurationDataMessage : IMessageEvent
{
    public required int BotId { get; init; }
    public required int CommandId { get; init; }
}
