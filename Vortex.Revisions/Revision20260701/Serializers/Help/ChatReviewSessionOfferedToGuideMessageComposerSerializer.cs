using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Help;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class ChatReviewSessionOfferedToGuideMessageComposerSerializer(int header)
    : AbstractSerializer<ChatReviewSessionOfferedToGuideMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ChatReviewSessionOfferedToGuideMessageComposer message
    ) => packet.WriteInteger(message.AcceptanceTimeoutSeconds);
}
