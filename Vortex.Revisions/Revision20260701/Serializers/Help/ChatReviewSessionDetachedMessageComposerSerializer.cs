using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Help;

namespace Vortex.Revisions.Revision20260701.Serializers.Help;

internal class ChatReviewSessionDetachedMessageComposerSerializer(int header)
    : AbstractSerializer<ChatReviewSessionDetachedMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ChatReviewSessionDetachedMessageComposer message
    )
    {
        //
    }
}
