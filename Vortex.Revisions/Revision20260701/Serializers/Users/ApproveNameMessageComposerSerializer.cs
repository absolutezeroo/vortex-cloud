using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Users;

internal class ApproveNameMessageComposerSerializer(int header)
    : AbstractSerializer<ApproveNameMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, ApproveNameMessageComposer message)
    {
        packet.WriteInteger(message.Result).WriteString(message.ValidationInfo);
    }
}
