using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Users;

public record GetSelectedBadgesMessage : IMessageEvent
{
    // GetSelectedBadgesMessageComposer(userId) — the profile whose equipped badges to fetch, header 3726.
    public required int UserId { get; init; }
}
