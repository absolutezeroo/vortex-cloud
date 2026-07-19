using Vortex.Primitives.Messages.Outgoing.Moderation;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Moderation;

internal class ModeratorActionResultMessageComposerSerializer(int header)
    : AbstractSerializer<ModeratorActionResultMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ModeratorActionResultMessageComposer message
    )
    {
        packet.WriteInteger(message.UserId).WriteBoolean(message.Success);
    }
}
