using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Avatar;

/// <summary>
/// Claims a name. Sent when the onboarding editor submits, after the check has passed.
/// </summary>
public record ChangeUserNameMessage : IMessageEvent
{
    public required string Name { get; init; }
}
