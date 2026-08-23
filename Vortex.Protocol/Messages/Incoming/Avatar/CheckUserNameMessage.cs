using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Avatar;

/// <summary>
/// Asks whether a name is free. Sent by the onboarding name dialog 500ms after the last keystroke.
/// </summary>
public record CheckUserNameMessage : IMessageEvent
{
    public required string Name { get; init; }
}
