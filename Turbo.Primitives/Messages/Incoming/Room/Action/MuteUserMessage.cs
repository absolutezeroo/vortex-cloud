using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Room.Action;

public record MuteUserMessage : IMessageEvent
{
    public required int UserId { get; init; }
    public int Minutes { get; init; }
    public int RoomId { get; init; }
}
