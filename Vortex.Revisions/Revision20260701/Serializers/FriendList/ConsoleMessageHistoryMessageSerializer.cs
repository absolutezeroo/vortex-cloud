using Vortex.Primitives.Messages.Outgoing.FriendList;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Snapshots.FriendList;

namespace Vortex.Revisions.Revision20260701.Serializers.FriendList;

internal class ConsoleMessageHistoryMessageSerializer(int header)
    : AbstractSerializer<ConsoleMessageHistoryMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ConsoleMessageHistoryMessageComposer message
    )
    {
        packet.WriteInteger(message.ChatId);
        packet.WriteInteger(message.Messages.Count);

        foreach (MessageHistoryEntrySnapshot consoleMessage in message.Messages)
        {
            packet.WriteInteger(consoleMessage.SenderId);
            packet.WriteString(consoleMessage.SenderName);
            packet.WriteString(consoleMessage.SenderFigure);
            packet.WriteString(consoleMessage.Message);
            packet.WriteInteger(consoleMessage.SecondsSinceSent);
            packet.WriteString(consoleMessage.MessageId);
        }
    }
}
