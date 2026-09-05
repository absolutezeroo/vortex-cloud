using Vortex.Primitives.Packets;
using Vortex.Primitives.Snapshots.FriendList;
using Vortex.Protocol.Messages.Outgoing.FriendList;

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
            // The client reads the body as a tagged union, not a string: an int discriminator,
            // then the text (0) or a habbicon id (1). See WIN63
            // src/unknowns/_SafePkg_1764/_SafeCls_3241.as::parse(). Writing the string alone
            // made the client take this integer's bytes as a length and misalign every field
            // after it. Exactly one arm is written, never both.
            if (consoleMessage.HabbiconId > 0)
            {
                packet.WriteInteger(1).WriteInteger(consoleMessage.HabbiconId);
            }
            else
            {
                packet.WriteInteger(0).WriteString(consoleMessage.Message);
            }

            packet.WriteInteger(consoleMessage.SecondsSinceSent);
            packet.WriteString(consoleMessage.MessageId);
        }
    }
}
