using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Primitives.Players;

public interface IBuildersClubService
{
    Task<BuildersClubSubscriptionSnapshot> GetSubscriptionAsync(
        int playerId,
        CancellationToken ct = default
    );

    /// <summary>Total furniture the player currently owns, account-wide (placed in rooms + in
    /// inventory) -- matches the WIN63 client's own BuildersClubFurniCountMessageEvent, which
    /// carries only the raw count (the client already knows the limit from the subscription-status
    /// push).</summary>
    Task<int> GetOwnedFurnitureCountAsync(int playerId, CancellationToken ct = default);
}
