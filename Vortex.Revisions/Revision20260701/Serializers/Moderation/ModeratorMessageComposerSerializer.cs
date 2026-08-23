using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Moderation;

namespace Vortex.Revisions.Revision20260701.Serializers.Moderation;

internal class ModeratorMessageComposerSerializer(int header)
    : AbstractSerializer<ModeratorMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, ModeratorMessageComposer message) =>
        packet.WriteString(message.Message).WriteString(message.Url);
}
