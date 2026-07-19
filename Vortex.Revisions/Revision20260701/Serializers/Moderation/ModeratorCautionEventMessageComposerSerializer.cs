using Vortex.Primitives.Messages.Outgoing.Moderation;
using Vortex.Primitives.Packets;

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
