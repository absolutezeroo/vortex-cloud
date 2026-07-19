using Vortex.Primitives.Messages.Outgoing.Help;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class GuideSessionPartnerIsTypingMessageComposerSerializer(int header)
    : AbstractSerializer<GuideSessionPartnerIsTypingMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GuideSessionPartnerIsTypingMessageComposer message
    )
    {
        //
    }
}
