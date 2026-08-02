using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Prizes.Admin;

namespace Vortex.Primitives.Prizes;

/// <summary>
/// Admin writes for the prize pools every reward furniture draws from. Every write reloads the
/// kept-alive cache in <see cref="Grains.IPrizePoolManagerGrain"/>, so an edit is live without a
/// restart.
/// </summary>
public interface IPrizePoolAdminService
{
    public Task<PrizeAdminResult> CreatePoolAsync(PrizePoolSpec spec, CancellationToken ct);

    public Task<PrizeAdminResult> UpdatePoolAsync(
        int poolId,
        PrizePoolSpec spec,
        CancellationToken ct
    );

    /// <summary>Deletes a pool and, by cascade, every entry weighted against it.</summary>
    public Task<PrizeAdminResult> DeletePoolAsync(int poolId, CancellationToken ct);

    public Task<PrizeAdminResult> CreateEntryAsync(PrizeEntrySpec spec, CancellationToken ct);

    public Task<PrizeAdminResult> UpdateEntryAsync(
        int entryId,
        PrizeEntrySpec spec,
        CancellationToken ct
    );

    public Task<PrizeAdminResult> DeleteEntryAsync(int entryId, CancellationToken ct);

    /// <summary>Points a furniture definition at the pool it draws from.</summary>
    public Task<PrizeAdminResult> CreateBindingAsync(PrizeBindingSpec spec, CancellationToken ct);

    public Task<PrizeAdminResult> UpdateBindingAsync(
        int bindingId,
        PrizeBindingSpec spec,
        CancellationToken ct
    );

    public Task<PrizeAdminResult> DeleteBindingAsync(int bindingId, CancellationToken ct);

    /// <summary>Rebuilds the live cache without changing anything.</summary>
    public Task<PrizeAdminResult> ReloadCacheAsync(CancellationToken ct);
}
