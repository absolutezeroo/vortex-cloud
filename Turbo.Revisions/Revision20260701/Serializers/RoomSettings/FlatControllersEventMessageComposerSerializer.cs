using Turbo.Primitives.Messages.Outgoing.Roomsettings;
using Turbo.Primitives.Orleans.Snapshots.Room;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260701.Serializers.RoomSettings;

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
