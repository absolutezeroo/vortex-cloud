using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Navigator;

namespace Vortex.Revisions.Revision20260701.Parsers.Navigator;

internal class RoomAdSearchMessageParser : IParser
{
    // Two ints (MainViewCtrl.as:657, `new _SafeCls_3386(_navigator.data.adIndex, param1)`). The
    // message record already declared both fields; nothing ever filled them, so they were
    // structurally always 0.
    public IMessageEvent Parse(IClientPacket packet) =>
        new RoomAdSearchMessage { AdIndex = packet.PopInt(), TabId = packet.PopInt() };
}
