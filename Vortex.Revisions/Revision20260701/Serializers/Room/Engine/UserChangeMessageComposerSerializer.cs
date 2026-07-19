using Vortex.Primitives.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Engine;

internal class UserChangeMessageComposerSerializer(int header)
    : AbstractSerializer<UserChangeMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, UserChangeMessageComposer message)
    {
        packet
            .WriteInteger(message.ObjectId)
            .WriteString(message.Figure)
            .WriteString(message.Gender.ToLegacyString())
            .WriteString(message.CustomInfo)
            .WriteInteger(message.AchievementScore);
    }
}
