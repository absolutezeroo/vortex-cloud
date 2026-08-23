using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Protocol.Messages.Incoming.RoomSettings;

namespace Vortex.Revisions.Revision20260701.Parsers.RoomSettings;

internal class UpdateRoomCategoryAndTradeSettingsMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new UpdateRoomCategoryAndTradeSettingsMessage
        {
            RoomId = packet.PopInt(),
            CategoryId = packet.PopInt(),
            TradeType = (RoomTradeModeType)packet.PopInt(),
        };
}
