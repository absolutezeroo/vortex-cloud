using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Help;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class GuideSessionEndedMessageComposerSerializer(int header)
    : AbstractSerializer<GuideSessionEndedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GuideSessionEndedMessageComposer message
    ) => packet.WriteInteger(message.EndReason);
}
