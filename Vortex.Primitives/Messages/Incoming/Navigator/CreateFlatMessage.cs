using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Primitives.Messages.Incoming.Navigator;

public record CreateFlatMessage : IMessageEvent
{
    public required string FlatName { get; init; }
    public required string FlatDescription { get; init; }
    public required string FlatModelName { get; init; }
    public int CategoryID { get; init; }
    public int MaxPlayers { get; init; }
    public RoomTradeModeType TradeSetting { get; init; }
}
