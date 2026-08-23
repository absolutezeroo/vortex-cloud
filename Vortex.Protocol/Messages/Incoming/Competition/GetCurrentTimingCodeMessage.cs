using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Competition;

public record GetCurrentTimingCodeMessage : IMessageEvent
{
    public required string SlotConfig { get; init; }
}
