using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Users;

namespace Vortex.Revisions.Revision20260701.Serializers.Users;

internal class UserNameChangedMessageComposerSerializer(int header)
    : AbstractSerializer<UserNameChangedMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, UserNameChangedMessageComposer message)
    {
        packet.WriteInteger(message.WebId).WriteInteger(message.Id).WriteString(message.Name);
    }
}
