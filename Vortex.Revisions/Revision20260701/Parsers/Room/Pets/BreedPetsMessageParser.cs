using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Protocol.Messages.Incoming.Room.Pets;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Pets;

internal class BreedPetsMessageParser : IParser
{
    // Three ints, not two. `_SafeCls_2980(action, petOne.webID, petTwo.webID)` is sent by all
    // three buttons with 0/1/2 in front (AvatarInfoWidget.as:1737-1764); reading only the last two
    // fields of a three-field message shifted both ids.
    public IMessageEvent Parse(IClientPacket packet) =>
        new BreedPetsMessage
        {
            Action = (PetBreedingActionType)packet.PopInt(),
            PetOneId = packet.PopInt(),
            PetTwoId = packet.PopInt(),
        };
}
