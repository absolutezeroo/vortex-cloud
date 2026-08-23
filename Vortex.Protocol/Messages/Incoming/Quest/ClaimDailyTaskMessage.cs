using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Quest;

/// <summary>
/// The player pressed claim on a completed daily task. The id goes out as a long and comes back as
/// an int — that asymmetry is the client's, not ours.
/// </summary>
public record ClaimDailyTaskMessage : IMessageEvent
{
    public required int TaskId { get; init; }
}
