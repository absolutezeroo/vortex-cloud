using Turbo.Primitives.Messages.Incoming.Room.Furniture;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260112.Parsers.Room.Furniture;

internal class GetRentableSpaceConfigMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new GetRentableSpaceConfigMessage { FurnitureId = packet.PopInt() };
}
