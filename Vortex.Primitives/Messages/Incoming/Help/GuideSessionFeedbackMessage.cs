using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Help;

/// <summary>
/// The requester's verdict on the session they have just finished: was the guide any help.
/// </summary>
public record GuideSessionFeedbackMessage : IMessageEvent
{
    public required bool WasHelpful { get; init; }
}
