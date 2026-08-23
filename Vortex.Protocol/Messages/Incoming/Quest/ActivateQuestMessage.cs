using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Quest;

public record ActivateQuestMessage : IMessageEvent
{
    public required int QuestId { get; init; }
}
