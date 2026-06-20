using Turbo.Primitives.Messages.Outgoing.Room.Engine;
using Turbo.Primitives.Packets;
using Turbo.Primitives.Rooms.Snapshots.Avatars;
using Turbo.Revisions.Revision20260112.Serializers.Room.Engine.Data;

namespace Turbo.Revisions.Revision20260112.Serializers.Room.Engine;

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
