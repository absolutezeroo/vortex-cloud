using Vortex.Primitives.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Snapshots.Avatars;
using Vortex.Revisions.Revision20260701.Serializers.Room.Engine.Data;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Engine;

internal class UsersMessageComposerSerializer(int header)
    : AbstractSerializer<UsersMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, UsersMessageComposer message)
    {
        packet.WriteInteger(message.Avatars.Length);

        foreach (RoomAvatarSnapshot avatar in message.Avatars)
        {
            RoomAvatarSerializer.Serialize(packet, avatar);
        }
    }
}
