using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Users;

public record GetExtendedProfileByNameMessage : IMessageEvent
{
    public required string UserName { get; init; }
}
