using Vortex.Primitives.Messages.Outgoing.Navigator;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Navigator;

internal class FlatAccessDeniedMessageComposerSerializer(int header)
    : AbstractSerializer<FlatAccessDeniedMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, FlatAccessDeniedMessageComposer message)
    {
        packet.WriteInteger(message.RoomId).WriteString(message.Username ?? string.Empty);
    }
}
