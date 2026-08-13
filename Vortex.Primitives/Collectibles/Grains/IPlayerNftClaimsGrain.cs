using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;

namespace Vortex.Primitives.Collectibles.Grains;

/// <summary>
/// One player's Relics — the prizes waiting to be collected from the Collectors Guild.
/// </summary>
/// <remarks>
/// Keyed by player rather than global, because that is the granularity the safety matters at: the
/// client's only button claims everything at once, so two quick clicks must not hand the same prize
/// over twice. A player-keyed grain is single-threaded per player, which settles that without
/// serialising every player in the hotel behind one lock.
/// </remarks>
public interface IPlayerNftClaimsGrain : IGrainWithIntegerKey
{
    /// <summary>
    /// What the Rewards tab lists. Fully-claimed rows are filtered out — the client hides them
    /// anyway, and a claim with nothing left is not a reward.
    /// </summary>
    /// <param name="wallet">Echoed back on each claim; the client shows it under the reward.</param>
    public Task<ImmutableArray<NftClaimSnapshot>> GetClaimsAsync(
        string wallet,
        CancellationToken ct
    );

    /// <summary>
    /// Takes everything outstanding, which is the only thing the client's button asks for. Returns
    /// how many prizes were actually handed over; zero means there was nothing to take.
    /// </summary>
    public Task<int> ClaimAllAsync(CancellationToken ct);
}
