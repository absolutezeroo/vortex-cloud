using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Collectibles;

/// <summary>
/// Taking one Relic back off the trade table.
/// </summary>
/// <remarks>
/// The same gesture that removes furniture: clicking a row in your own offer. The client sends
/// <c>RemoveItemFromTrade</c> for rows inside the furniture list and this for the ones past it, so
/// the two messages are one user action split by what the row happens to hold.
/// </remarks>
public record RemoveNftFromTradeMessage : IMessageEvent
{
    /// <summary>
    /// The asset id, not the row. The client groups Relics by product code over a vector of asset
    /// ids and sends one of those, which is the same key <see cref="AddNftToTradeMessage"/> uses.
    /// </summary>
    public required int AssetId { get; init; }
}
