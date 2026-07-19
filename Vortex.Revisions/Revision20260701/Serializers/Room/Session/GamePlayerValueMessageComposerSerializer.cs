using Vortex.Primitives.Messages.Outgoing.Room.Session;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Session;

internal class GamePlayerValueMessageComposerSerializer(int header)
    : AbstractSerializer<GamePlayerValueMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, GamePlayerValueMessageComposer message)
    {
        packet.WriteInteger(message.UserId).WriteInteger(message.Value);
    }
}
