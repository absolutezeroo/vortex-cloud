using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Players;

namespace Vortex.Primitives.Rooms.Object;

/// <summary>
/// The contract transactions waiting on players in this room.
/// </summary>
/// <remarks>
/// In-process, like the other room accesses: wired logic runs inside the room's own activation and
/// must not call the grain it is already on.
/// </remarks>
public interface IRoomTransactionAccess
{
    /// <summary>
    /// Offers a contract to a player, replacing whatever was waiting on them.
    /// </summary>
    /// <remarks>
    /// One at a time per player: the client shows a single trading screen, so a second offer is the
    /// first one being withdrawn. Returns false when the box points at no contract.
    /// </remarks>
    Task<bool> OfferTransactionAsync(
        int contractId,
        PlayerId playerId,
        int mode,
        int multiplier,
        int timeoutSeconds,
        CancellationToken ct
    );

    /// <summary>
    /// Calls off what is waiting on the resolved users, and raises the failure trigger for each one
    /// actually cancelled.
    /// </summary>
    /// <param name="contractId">
    /// The contract to match, or 0 for any transaction the player has open — the client's two
    /// choices, "specified contract" and "any ongoing transaction".
    /// </param>
    Task<int> CancelTransactionAsync(int contractId, PlayerId playerId, CancellationToken ct);
}
