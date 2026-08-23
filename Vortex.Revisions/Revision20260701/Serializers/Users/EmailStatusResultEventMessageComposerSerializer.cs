using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Users;

namespace Vortex.Revisions.Revision20260701.Serializers.Users;

internal class EmailStatusResultEventMessageComposerSerializer(int header)
    : AbstractSerializer<EmailStatusResultEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        EmailStatusResultEventMessageComposer message
    )
    {
        packet
            .WriteString(message.Email)
            .WriteBoolean(message.IsVerified)
            .WriteBoolean(message.AllowChange);
    }
}
