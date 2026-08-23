using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Quest;

public record AcceptQuestMessage : IMessageEvent
{
    public required int QuestId { get; init; }
}
