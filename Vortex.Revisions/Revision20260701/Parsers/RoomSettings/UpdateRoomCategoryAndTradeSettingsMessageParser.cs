using Vortex.Primitives.Messages.Incoming.RoomSettings;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Enums;

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
