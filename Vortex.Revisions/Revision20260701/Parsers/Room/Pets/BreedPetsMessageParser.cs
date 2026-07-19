using Vortex.Primitives.Messages.Incoming.Room.Pets;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Pets;

internal class BreedPetsMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new BreedPetsMessage { PetOneId = packet.PopInt(), PetTwoId = packet.PopInt() };
}
