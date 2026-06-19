using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Users;

public record SelectFavouriteHabboGroupMessage : IMessageEvent
{
    public required int GroupId { get; init; }
}
