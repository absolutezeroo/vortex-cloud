using Vortex.Primitives.Messages.Incoming.Inventory.Pets;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Inventory.Pets;

internal class CancelPetBreedingMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new CancelPetBreedingMessage { PetId = packet.PopInt() };
}
