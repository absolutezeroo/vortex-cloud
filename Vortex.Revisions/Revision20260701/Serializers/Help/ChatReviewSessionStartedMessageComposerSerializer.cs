using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Help;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class ChatReviewSessionStartedMessageComposerSerializer(int header)
    : AbstractSerializer<ChatReviewSessionStartedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ChatReviewSessionStartedMessageComposer message
    ) => packet.WriteInteger(message.VotingTimeoutSeconds).WriteString(message.ChatRecord);
}
