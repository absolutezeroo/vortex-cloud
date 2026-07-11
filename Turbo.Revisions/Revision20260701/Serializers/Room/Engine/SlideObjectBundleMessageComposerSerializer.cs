using Turbo.Primitives.Messages.Outgoing.Room.Engine;
using Turbo.Primitives.Packets;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Primitives.Rooms.Object;

namespace Turbo.Revisions.Revision20260701.Serializers.Room.Engine;

internal class SlideObjectBundleMessageComposerSerializer(int header)
    : AbstractSerializer<SlideObjectBundleMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        SlideObjectBundleMessageComposer message
    )
    {
        packet
            .WriteInteger(message.FromX)
            .WriteInteger(message.FromY)
            .WriteInteger(message.ToX)
            .WriteInteger(message.ToY)
            .WriteInteger(message.FloorItemHeights.Length);

        foreach ((int objectId, Altitude prev, Altitude next) in message.FloorItemHeights)
        {
            packet.WriteInteger(objectId).WriteString(prev.ToString()).WriteString(next.ToString());
        }

        packet.WriteInteger(message.RollerItemId);

        if (message.Avatar is not null)
        {
            (SlideAvatarMoveType moveType, int objectId, Altitude prev, Altitude next) = message
                .Avatar
                .Value;

            packet
                .WriteInteger((int)moveType)
                .WriteInteger(objectId)
                .WriteString(prev.ToString())
                .WriteString(next.ToString());
        }
    }
}
