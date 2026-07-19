using Vortex.Primitives.Messages.Outgoing.FriendList;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.FriendList;

internal class RoomInviteErrorMessageSerializer(int header)
    : AbstractSerializer<RoomInviteErrorMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, RoomInviteErrorMessageComposer message)
    {
        packet.WriteInteger(message.ErrorCode);

        if (message.ErrorCode is 1)
        {
            packet.WriteInteger(message.FailedRecipients?.Count ?? 0);

            if (message.FailedRecipients is not null)
            {
                foreach (int recipient in message.FailedRecipients)
                {
                    packet.WriteInteger(recipient);
                }
            }
        }
    }
}
