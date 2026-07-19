using Vortex.Primitives.Messages.Incoming.Navigator;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Navigator;

internal class ToggleStaffPickMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new ToggleStaffPickMessage
        {
            RoomId = packet.PopInt(),
            IsStaffPicked = packet.PopBoolean(),
        };
}
