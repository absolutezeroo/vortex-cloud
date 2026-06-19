using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Users;

public record GetGuildEditInfoMessage : IMessageEvent
{
    public required int GroupId { get; init; }
}
