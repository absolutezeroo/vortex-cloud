using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Poll;

/// <summary>The player declined the offer dialog.</summary>
public record PollRejectMessage : IMessageEvent
{
    public required int PollId { get; init; }
}
