using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Hotel.Contracts;

/// <summary>
/// Move one item from the player who owns it to another.
/// </summary>
/// <param name="PlayerId">The current owner, checked against the row: an item id alone would let a
/// typo move a stranger's furniture.</param>
public sealed record TransferFurnitureRequest(
    int PlayerId,
    int ToPlayerId,
    int ItemId,
    string Reason
) : IReasonedRequest;
