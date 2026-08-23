using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Catalog;

public record SelectClubGiftMessage : IMessageEvent
{
    public string? ProductCode { get; init; }
}
