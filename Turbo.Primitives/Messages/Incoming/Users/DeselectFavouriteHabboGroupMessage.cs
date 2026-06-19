using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Users;

public record DeselectFavouriteHabboGroupMessage : IMessageEvent
{
    public required int GroupId { get; init; }
}
