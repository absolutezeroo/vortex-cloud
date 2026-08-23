using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Groupforums;

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
