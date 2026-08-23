using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Protocol.Messages.Incoming.Navigator;

namespace Vortex.Revisions.Revision20260701.Parsers.Navigator;

internal class CreateFlatMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new CreateFlatMessage
        {
            FlatName = packet.PopString(),
            FlatDescription = packet.PopString(),
            FlatModelName = packet.PopString(),
            CategoryID = packet.PopInt(),
            MaxPlayers = packet.PopInt(),
            TradeSetting = (RoomTradeModeType)packet.PopInt(),
        };
}
