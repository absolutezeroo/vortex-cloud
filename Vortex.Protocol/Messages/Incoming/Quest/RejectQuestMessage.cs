using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Quest;

public record RejectQuestMessage : IMessageEvent
{
    public required int QuestId { get; init; }
}
