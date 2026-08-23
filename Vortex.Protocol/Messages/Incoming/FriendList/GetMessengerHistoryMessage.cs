using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.FriendList;

public record GetMessengerHistoryMessage : IMessageEvent
{
    public int ChatId { get; init; }
    public required string Message { get; init; }
}
