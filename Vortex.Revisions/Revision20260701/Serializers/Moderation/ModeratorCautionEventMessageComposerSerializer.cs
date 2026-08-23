using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Moderation;

namespace Vortex.Revisions.Revision20260701.Serializers.Moderation;

internal class ModeratorCautionEventMessageComposerSerializer(int header)
    : AbstractSerializer<ModeratorCautionEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ModeratorCautionEventMessageComposer message
    )
    {
        packet.WriteString(message.Message).WriteString(message.Url);
    }
}
