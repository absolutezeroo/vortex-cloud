using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Help;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class GuideSessionInvitedToGuideRoomMessageComposerSerializer(int header)
    : AbstractSerializer<GuideSessionInvitedToGuideRoomMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GuideSessionInvitedToGuideRoomMessageComposer message
    ) => packet.WriteInteger(message.RoomId).WriteString(message.RoomName);
}
