using Vortex.Protocol.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Engine;

internal class ObjectRemoveConfirmMessageComposerSerializer(int header)
    : AbstractSerializer<ObjectRemoveConfirmMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        ObjectRemoveConfirmMessageComposer message
    )
    {
        packet
            .WriteInteger(message.IsWallItem)
            .WriteInteger(message.ObjectId)
            .WriteString(message.ConfirmTitle)
            .WriteString(message.ConfirmBody);
    }
}
