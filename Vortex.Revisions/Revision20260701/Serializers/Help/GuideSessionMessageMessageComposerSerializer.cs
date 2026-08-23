using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Help;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class GuideSessionMessageMessageComposerSerializer(int header)
    : AbstractSerializer<GuideSessionMessageMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GuideSessionMessageMessageComposer message
    ) => packet.WriteString(message.ChatMessage).WriteInteger(message.SenderId);
}
