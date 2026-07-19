using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Users;

internal class GuildEditFailedMessageComposerSerializer(int header)
    : AbstractSerializer<GuildEditFailedMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, GuildEditFailedMessageComposer message)
    {
        packet.WriteInteger(message.Reason);
    }
}
