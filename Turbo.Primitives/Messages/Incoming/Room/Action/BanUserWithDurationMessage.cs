using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Room.Action;

public record BanUserWithDurationMessage : IMessageEvent
{
    public required int UserId { get; init; }
    public int RoomId { get; init; }
    public required string BanType { get; init; }
}
