using Vortex.Primitives.Messages.Outgoing.Help;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class ChatReviewSessionOfferedToGuideMessageComposerSerializer(int header)
    : AbstractSerializer<ChatReviewSessionOfferedToGuideMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ChatReviewSessionOfferedToGuideMessageComposer message
    )
    {
        //
    }
}
