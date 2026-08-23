using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Navigator;

public record GuildBaseSearchMessage : IMessageEvent
{
    public int Unknown { get; init; }
}
