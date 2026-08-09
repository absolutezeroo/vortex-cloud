using Vortex.Primitives.Messages.Incoming.Room.Avatar;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Avatar;

internal class PassCarryItemToPetMessageParser : IParser
{
    // Which pet is being fed (InfoStandWidgetHandler RWUAM_GIVE_CARRY_ITEM_TO_PET).
    public IMessageEvent Parse(IClientPacket packet) =>
        new PassCarryItemToPetMessage { PetId = packet.PopInt() };
}
