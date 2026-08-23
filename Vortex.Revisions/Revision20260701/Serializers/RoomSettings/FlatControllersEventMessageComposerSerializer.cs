using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Roomsettings;

namespace Vortex.Revisions.Revision20260701.Serializers.RoomSettings;

internal class FlatControllersEventMessageComposerSerializer(int header)
    : AbstractSerializer<FlatControllersEventMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        FlatControllersEventMessageComposer message
    )
    {
        packet.WriteInteger(message.RoomId).WriteInteger(message.Controllers.Length);

        foreach (RoomControllerSnapshot controller in message.Controllers)
        {
            packet.WriteInteger(controller.PlayerId).WriteString(controller.Name);
        }
    }
}
