using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Help;

/// <summary>
/// The typing indicator. Sent only when the state flips, not on every keystroke — the client keeps
/// the last value it sent and compares.
/// </summary>
public record GuideSessionIsTypingMessage : IMessageEvent
{
    public required bool IsTyping { get; init; }
}
