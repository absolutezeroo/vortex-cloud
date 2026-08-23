using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Users;

namespace Vortex.Revisions.Revision20260701.Serializers.Users;

internal class IgnoredUsersMessageComposerSerializer(int header)
    : AbstractSerializer<IgnoredUsersMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, IgnoredUsersMessageComposer message)
    {
        packet.WriteInteger(message.IgnoredUserIds.Count);

        foreach (int userId in message.IgnoredUserIds)
        {
            packet.WriteInteger(userId);
        }
    }
}
