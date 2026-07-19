using Vortex.Primitives.Messages.Incoming.Room.Furniture;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Furniture;

internal class ConfigureRentableSpaceMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new ConfigureRentableSpaceMessage
        {
            FurnitureId = packet.PopInt(),
            Price = packet.PopInt(),
            CurrencyTypeId = packet.PopInt(),
            RentDurationSeconds = packet.PopInt(),
            RequiresHc = packet.PopBoolean(),
        };
}
