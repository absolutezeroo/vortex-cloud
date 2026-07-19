using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Help;

public record GetCfhStatusMessage : IMessageEvent
{
    public bool Flag { get; init; }
}
