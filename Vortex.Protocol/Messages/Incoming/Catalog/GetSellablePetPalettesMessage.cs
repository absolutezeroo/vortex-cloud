using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Catalog;

public record GetSellablePetPalettesMessage : IMessageEvent
{
    public string LocalizationId { get; init; } = string.Empty;
}
