using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Users;

public record GetExtendedProfileByNameMessage : IMessageEvent
{
    public required string UserName { get; init; }
}
