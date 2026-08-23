using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Outgoing.Room.Furniture;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Furniture;

internal class PresentOpenedMessageComposerSerializer(int header)
    : AbstractSerializer<PresentOpenedMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, PresentOpenedMessageComposer message)
    {
        packet
            .WriteString(message.ItemType)
            .WriteInteger(message.ClassId)
            .WriteString(message.ProductCode.ToLegacyString())
            .WriteInteger(message.PlacedItemId)
            .WriteString(message.PlacedItemType.ToLegacyString())
            .WriteBoolean(message.PlacedInRoom)
            .WriteString(message.PetFigureString);
    }
}
