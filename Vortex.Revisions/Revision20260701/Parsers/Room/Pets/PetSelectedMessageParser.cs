using Vortex.Primitives.Messages.Incoming.Room.Pets;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Pets;

internal class PetSelectedMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new PetSelectedMessage { PetId = packet.PopInt() };
}
