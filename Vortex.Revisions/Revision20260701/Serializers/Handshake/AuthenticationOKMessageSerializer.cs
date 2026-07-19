using Vortex.Primitives.Messages.Outgoing.Handshake;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Handshake;

internal class AuthenticationOKMessageSerializer(int header)
    : AbstractSerializer<AuthenticationOKMessage>(header)
{
    protected override void Serialize(IServerPacket packet, AuthenticationOKMessage message)
    {
        packet.WriteInteger(message.AccountId);
        packet.WriteInteger(message.SuggestedLoginActions?.Length ?? 0);

        if (message.SuggestedLoginActions != null)
        {
            foreach (short action in message.SuggestedLoginActions)
            {
                packet.WriteShort(action);
            }
        }

        packet.WriteInteger(message.IdentityId);
    }
}
