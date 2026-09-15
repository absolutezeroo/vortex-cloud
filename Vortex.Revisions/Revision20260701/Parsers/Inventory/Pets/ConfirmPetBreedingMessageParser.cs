using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Inventory.Pets;

namespace Vortex.Revisions.Revision20260701.Parsers.Inventory.Pets;

internal class ConfirmPetBreedingMessageParser : IParser
{
    // Four fields: `_SafeCls_3418(stuffId:int, name:String, petOne:int, petTwo:int)`. Reading only
    // the first left the name and both pet ids on the wire.
    public IMessageEvent Parse(IClientPacket packet) =>
        new ConfirmPetBreedingMessage
        {
            NestStuffId = packet.PopInt(),
            PetName = packet.PopString(),
            PetOneId = packet.PopInt(),
            PetTwoId = packet.PopInt(),
        };
}
