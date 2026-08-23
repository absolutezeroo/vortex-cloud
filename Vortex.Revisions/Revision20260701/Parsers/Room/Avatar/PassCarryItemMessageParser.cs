using Vortex.Protocol.Messages.Incoming.Room.Avatar;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Room.Avatar;

internal class PassCarryItemMessageParser : IParser
{
    // The client sends who is being handed the item (InfoStandWidgetHandler RWUAM_PASS_CARRY_ITEM);
    // reading nothing left every pass aimed at nobody.
    public IMessageEvent Parse(IClientPacket packet) =>
        new PassCarryItemMessage { TargetPlayerId = packet.PopInt() };
}
