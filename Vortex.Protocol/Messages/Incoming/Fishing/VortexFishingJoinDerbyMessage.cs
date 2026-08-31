using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Fishing;

/// <summary>
/// Enter the running fishing derby. Vortex-specific: no AS3 or Habbo equivalent, and Vortex's own
/// addition rather than a reconstruction — Origins has the Fishing Frenzy, not a leaderboard.
/// </summary>
/// <remarks>
/// Names the derby the client believes is running, which is what lets the server refuse a stale
/// click with <c>DerbyClosed</c> instead of silently entering the player into a different contest.
/// </remarks>
public record VortexFishingJoinDerbyMessage : IMessageEvent
{
    public required int DerbyId { get; init; }
}
