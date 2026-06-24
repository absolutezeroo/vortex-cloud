using Turbo.Primitives.Messages.Incoming.Room.Furniture;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Parsers.Room.Furniture;

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
