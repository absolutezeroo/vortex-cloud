using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Navigator;

public record GuildBaseSearchMessage : IMessageEvent
{
    public int Unknown { get; init; }
}
