using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Primitives.Messages.Incoming.RoomSettings;

public record UpdateRoomCategoryAndTradeSettingsMessage : IMessageEvent
{
    public RoomId RoomId { get; init; }
    public int CategoryId { get; init; }
    public RoomTradeModeType TradeType { get; init; }
}
