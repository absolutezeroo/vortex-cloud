using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Catalog;

public record ChargeFireworkMessage : IMessageEvent
{
    public int SpriteId { get; init; }
    public int Type { get; init; }
}
