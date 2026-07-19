using Vortex.Primitives.Messages.Outgoing.Help;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class GuideSessionInvitedToGuideRoomMessageComposerSerializer(int header)
    : AbstractSerializer<GuideSessionInvitedToGuideRoomMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        GuideSessionInvitedToGuideRoomMessageComposer message
    )
    {
        //
    }
}
