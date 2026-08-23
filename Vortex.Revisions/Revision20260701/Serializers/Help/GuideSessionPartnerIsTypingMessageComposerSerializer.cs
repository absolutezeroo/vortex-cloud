using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Help;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class GuideSessionPartnerIsTypingMessageComposerSerializer(int header)
    : AbstractSerializer<GuideSessionPartnerIsTypingMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GuideSessionPartnerIsTypingMessageComposer message
    ) => packet.WriteBoolean(message.IsTyping);
}
