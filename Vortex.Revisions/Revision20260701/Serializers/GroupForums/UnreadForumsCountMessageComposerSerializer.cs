using Vortex.Primitives.Messages.Outgoing.Groupforums;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.GroupForums;

internal class UnreadForumsCountMessageComposerSerializer(int header)
    : AbstractSerializer<UnreadForumsCountMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        UnreadForumsCountMessageComposer message
    )
    {
        packet.WriteInteger(message.UnreadForumsCount);
    }
}
