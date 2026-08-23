using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Inventory.Pets;

namespace Vortex.Revisions.Revision20260701.Parsers.Inventory.Pets;

internal class ConfirmPetBreedingMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new ConfirmPetBreedingMessage { PetId = packet.PopInt() };
}
