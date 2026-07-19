using Vortex.Primitives.Networking;
using Vortex.Primitives.Players;

namespace Vortex.Primitives.Messages.Incoming.Users;

public record GetExtendedProfileMessage : IMessageEvent
{
    public required PlayerId UserId { get; init; }
}
