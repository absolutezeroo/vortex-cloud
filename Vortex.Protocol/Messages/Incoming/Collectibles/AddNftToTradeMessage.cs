using System.Collections.Generic;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Collectibles;

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
/// Additive. <see cref="RemoveNftFromTradeMessage"/> is the counterpart, on its own header — the
/// same row click that removes furniture, taken past the end of the furniture list.
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
