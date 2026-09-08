using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Hotel.Contracts;

/// <summary>
/// Take one item back off a player.
/// </summary>
/// <param name="PlayerId">Who is losing it. Checked against the row rather than trusted: an item id
/// alone would let a typo delete a stranger's furniture.</param>
public sealed record RevokeFurnitureRequest(int PlayerId, int ItemId, string Reason)
    : IReasonedRequest;
