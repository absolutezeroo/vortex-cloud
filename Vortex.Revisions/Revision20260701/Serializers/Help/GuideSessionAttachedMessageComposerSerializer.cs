using Vortex.Protocol.Messages.Outgoing.Help;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class GuideSessionAttachedMessageComposerSerializer(int header)
    : AbstractSerializer<GuideSessionAttachedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GuideSessionAttachedMessageComposer message
    )
    {
        packet
            .WriteBoolean(message.AsGuide)
            .WriteInteger(message.HelpRequestType)
            .WriteString(message.HelpRequestDescription)
            .WriteInteger(message.RoleSpecificWaitTime);
    }
}
