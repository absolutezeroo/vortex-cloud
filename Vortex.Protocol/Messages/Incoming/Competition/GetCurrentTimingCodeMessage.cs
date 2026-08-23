using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Competition;

public record GetCurrentTimingCodeMessage : IMessageEvent
{
    public required string SlotConfig { get; init; }
}
