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
                // NO jumpingPower int here. The two WIN63 builds disagree: 202607011411 reads one
                // between the body rotation and the status
                // (_SafePkg_2184/_SafeCls_2826.parse:85), 202601121721 does not
                // (_SafePkg_2072/_SafeCls_3051.parse:82-84). Writing it killed the walk animation
                // on the live hotel, which serves the January build, so the 8-field shape stays
                // until the served build is settled. avatar.JumpPower is carried and unused.
                .WriteString(avatar.Status);
        }
    }
}
