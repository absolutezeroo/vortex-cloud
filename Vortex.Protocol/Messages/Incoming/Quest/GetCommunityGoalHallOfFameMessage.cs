using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Quest;

public record GetCommunityGoalHallOfFameMessage : IMessageEvent
{
    /// <summary>
    /// Which goal's board to show. The widget echoes back the code it was just handed by the
    /// timing-code event and only asks at all when that code is non-empty
    /// (CommunityGoalHallOfFameWidget.as:80-86).
    /// </summary>
    public string GoalCode { get; init; } = string.Empty;
}
