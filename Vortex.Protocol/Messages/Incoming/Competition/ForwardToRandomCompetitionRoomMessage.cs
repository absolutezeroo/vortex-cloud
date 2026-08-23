using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Competition;

/// <summary>
/// "Take me to a room in this competition", from the landing view.
/// </summary>
public record ForwardToRandomCompetitionRoomMessage : IMessageEvent
{
    /// <summary>Which competition — the goal code the landing-view widget was built with. The
    /// parser used to read nothing, so every one of these asked for a random room in no particular
    /// competition.</summary>
    public required string GoalCode { get; init; }
}
