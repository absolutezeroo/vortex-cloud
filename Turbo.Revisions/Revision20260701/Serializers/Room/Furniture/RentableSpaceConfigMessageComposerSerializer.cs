using Turbo.Primitives.Messages.Outgoing.Room.Furniture;
using Turbo.Primitives.Packets;
using Turbo.Primitives.Rooms.Snapshots;

namespace Turbo.Revisions.Revision20260701.Serializers.Room.Furniture;

internal class RentableSpaceConfigMessageComposerSerializer(int header)
    : AbstractSerializer<RentableSpaceConfigMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        RentableSpaceConfigMessageComposer message
    )
    {
        packet
            .WriteInteger(message.FurnitureId)
            .WriteBoolean(message.IsConfigured)
            .WriteInteger(message.Price)
            .WriteInteger(message.CurrencyTypeId)
            .WriteInteger(message.RentDurationSeconds)
            .WriteBoolean(message.RequiresHc)
            .WriteInteger(message.AvailableCurrencies.Count);

        foreach (AvailableCurrencySnapshot currency in message.AvailableCurrencies)
        {
            packet.WriteInteger(currency.Id).WriteString(currency.Name);
        }
    }
}
