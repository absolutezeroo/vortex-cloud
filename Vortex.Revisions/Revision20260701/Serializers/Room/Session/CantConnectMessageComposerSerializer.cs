using Vortex.Primitives.Messages.Outgoing.Room.Session;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Session;

internal class CantConnectMessageComposerSerializer(int header)
    : AbstractSerializer<CantConnectMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, CantConnectMessageComposer message)
    {
        packet.WriteInteger((int)message.ErrorType);

        if (message.ErrorType == RoomConnectionErrorType.Banned)
        {
            packet.WriteString(message.AdditionalInfo ?? string.Empty);
        }
    }
}
