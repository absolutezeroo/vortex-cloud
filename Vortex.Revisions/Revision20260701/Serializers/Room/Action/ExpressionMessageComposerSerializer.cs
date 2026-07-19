using Vortex.Primitives.Messages.Outgoing.Room.Action;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Action;

internal class ExpressionMessageComposerSerializer(int header)
    : AbstractSerializer<ExpressionMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, ExpressionMessageComposer message)
    {
        packet.WriteInteger(message.ObjectId).WriteInteger((int)message.ExpressionType);
    }
}
