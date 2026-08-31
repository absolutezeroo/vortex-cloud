using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Players;

namespace Vortex.Primitives.Fishing.Grains;

/// <summary>
/// The hotel's fishing derby — the running one, and the leaderboard behind it.
/// </summary>
/// <remarks>
/// <para>
/// A singleton: one derby runs at a time, and the leaderboard is hotel-wide. Vortex's own addition,
/// not an Origins feature — see <see cref="FishingDerbySnapshot"/>.
/// </para>
/// <para>
/// Every catch is offered here, whether or not the player joined; a player who did not join scores
/// nothing and the call returns immediately. Doing it the other way round would mean the session
/// grain has to know about derbies before it can hand one a catch.
/// </para>
/// </remarks>
public interface IFishingDerbyGrain : IGrainWithStringKey
{
    /// <summary>The running derby, or null when none is. Null is the ordinary state.</summary>
    Task<FishingDerbySnapshot?> GetCurrentAsync(CancellationToken ct);

    /// <summary>
    /// Registers a player for a derby and pushes them the standings. Answers false — and pushes
    /// <see cref="FishingErrorCode.DerbyClosed"/> — when the derby named is not the running one.
    /// </summary>
    Task<bool> JoinAsync(PlayerId playerId, int derbyId, CancellationToken ct);

    /// <summary>
    /// Offers one catch to the running derby. Does nothing unless a derby is running, the player
    /// joined it, and the weight beats what they already have.
    /// </summary>
    Task OfferCatchAsync(PlayerId playerId, int weight, CancellationToken ct);
}
