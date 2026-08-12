using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Poll;

/// <summary>The player accepted the offer dialog and wants the questions.</summary>
public record PollStartMessage : IMessageEvent
{
    public required int PollId { get; init; }
}
