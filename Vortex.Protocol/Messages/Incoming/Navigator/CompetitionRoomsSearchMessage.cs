using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Navigator;

public record CompetitionRoomsSearchMessage : IMessageEvent
{
    public int GoalId { get; init; }
    public int PageIndex { get; init; }
}
