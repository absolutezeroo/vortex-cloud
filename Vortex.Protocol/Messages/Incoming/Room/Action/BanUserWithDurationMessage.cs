using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Room.Action;

public record BanUserWithDurationMessage : IMessageEvent
{
    public required int UserId { get; init; }
    public int RoomId { get; init; }
    public required string BanType { get; init; }
}
