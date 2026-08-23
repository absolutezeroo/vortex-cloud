using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Help;

/// <summary>
/// A guardian's verdict: 0 for a chat that was fine, 1 for one that was not — the client's two
/// buttons, vote_ok and vote_bad.
/// </summary>
public record ChatReviewGuideVoteMessage : IMessageEvent
{
    public required int Vote { get; init; }
}
