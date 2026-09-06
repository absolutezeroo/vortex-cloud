using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Snapshots.Avatars;
using Vortex.Protocol.Messages.Outgoing.Room.Engine;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Engine;

internal class UserUpdateMessageComposerSerializer(int header)
    : AbstractSerializer<UserUpdateMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, UserUpdateMessageComposer message)
    {
        packet.WriteInteger(message.Avatars.Length);

        foreach (RoomAvatarSnapshot avatar in message.Avatars)
        {
            packet
                .WriteInteger(avatar.ObjectId)
                .WriteInteger(avatar.X)
                .WriteInteger(avatar.Y)
                .WriteString(avatar.Z.ToString())
                .WriteInteger((int)avatar.HeadRotation)
                .WriteInteger((int)avatar.BodyRotation)
                // jumpingPower — WIN63 reads a plain int here, before the status string
                // (_SafePkg_2184/_SafeCls_2826.parse → _SafeCls_3690.jumpingPower). Omitting it
                // shifted the status string into that int and mis-framed the whole packet.
                // Nothing in Vortex produces a jump yet; 0 is "not jumping".
                .WriteInteger(0)
                .WriteString(avatar.Status);
        }
    }
}
