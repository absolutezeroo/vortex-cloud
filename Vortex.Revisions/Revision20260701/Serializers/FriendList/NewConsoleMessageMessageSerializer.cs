using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.FriendList;

namespace Vortex.Revisions.Revision20260701.Serializers.FriendList;

internal class NewConsoleMessageMessageSerializer(int header)
    : AbstractSerializer<NewConsoleMessageMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        NewConsoleMessageMessageComposer message
    )
    {
        packet.WriteInteger(message.ChatId);
        // The client reads the body as a tagged union, not a string: an int discriminator,
        // then the text (0) or a habbicon id (1). See WIN63
        // src/unknowns/_SafePkg_1764/_SafeCls_3241.as::parse(). Writing the string alone
        // made the client take this integer's bytes as a length and misalign every field
        // after it. Zero is the text kind; the emulator has no habbicons to send.
        packet.WriteInteger(0);
        packet.WriteString(message.Message);
        packet.WriteInteger(message.SecondsSinceSent);
        packet.WriteString(message.MessageId);
        packet.WriteInteger(message.ConfirmationId);
        packet.WriteInteger(message.SenderId);
        packet.WriteString(message.SenderName);
        packet.WriteString(message.SenderFigure);
    }
}
