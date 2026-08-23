using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Room.Pets;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Pets;

internal class CompostPlantMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new CompostPlantMessage { PetId = packet.PopInt() };
}
