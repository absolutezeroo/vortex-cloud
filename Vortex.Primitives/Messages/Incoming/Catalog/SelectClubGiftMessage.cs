using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Catalog;

public record SelectClubGiftMessage : IMessageEvent
{
    public string? ProductCode { get; init; }
}
