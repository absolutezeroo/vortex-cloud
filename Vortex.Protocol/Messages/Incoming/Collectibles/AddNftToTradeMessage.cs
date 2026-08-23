using System.Collections.Generic;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Collectibles;

/// <summary>
/// Putting Relics into an open trade.
/// </summary>
/// <remarks>
/// <para>
/// This is what replaces the wallet transfer. The Transfer tab moves a whole wallet to one address
/// and cannot pick anything; the trade window moves <em>these</em> Relics to <em>this</em> player,
/// with the confirmation flow both sides already know.
/// </para>
/// <para>
/// Additive, and there is no counterpart that takes one back out — the client has no such message,
/// it re-derives which of its Relics are locked from the list the server sends back. So an offered
/// Relic stays offered until the trade ends.
/// </para>
/// </remarks>
public record AddNftToTradeMessage : IMessageEvent
{
    /// <summary>
    /// The asset ids offered. The client holds them as <c>Number</c> (its assets carry a long id)
    /// and casts each one down to an int before sending, so this is what actually arrives.
    /// </summary>
    public required IReadOnlyList<int> AssetIds { get; init; }
}
