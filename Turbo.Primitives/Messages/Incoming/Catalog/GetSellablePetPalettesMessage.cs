using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Catalog;

public record GetSellablePetPalettesMessage : IMessageEvent
{
    public string LocalizationId { get; init; } = string.Empty;
}
