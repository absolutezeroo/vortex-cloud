using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Competition;

/// <summary>Asks whether the player is entered in the competition behind a goal code.</summary>
public record GetIsUserPartOfCompetitionMessage : IMessageEvent
{
    /// <summary>The goal the landing-view widget is asking about; the client calls it goalCode.</summary>
    public required string GoalCode { get; init; }
}
