using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Users;

public record SelectFavouriteHabboGroupMessage : IMessageEvent
{
    public required int GroupId { get; init; }
}
