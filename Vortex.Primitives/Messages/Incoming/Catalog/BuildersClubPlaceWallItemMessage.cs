using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Catalog;

public record BuildersClubPlaceWallItemMessage : IMessageEvent
{
    public int PageId { get; init; }
    public int OfferId { get; init; }
    public string? ExtraParam { get; init; }
    public string? Location { get; init; }
    public bool ConfirmHideRoom { get; init; }
}
