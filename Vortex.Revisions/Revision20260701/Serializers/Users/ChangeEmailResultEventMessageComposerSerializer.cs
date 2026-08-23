using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Users;

namespace Vortex.Revisions.Revision20260701.Serializers.Users;

internal class ChangeEmailResultEventMessageComposerSerializer(int header)
    : AbstractSerializer<ChangeEmailResultEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ChangeEmailResultEventMessageComposer message
    )
    {
        packet.WriteInteger(message.Result);
    }
}
