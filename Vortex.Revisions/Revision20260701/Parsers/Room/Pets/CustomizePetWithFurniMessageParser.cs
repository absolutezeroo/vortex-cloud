using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Room.Pets;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Pets;

internal class CustomizePetWithFurniMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new CustomizePetWithFurniMessage { FurniItemId = packet.PopInt(), PetId = packet.PopInt() };
}
