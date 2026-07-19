using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Players;

namespace Vortex.Primitives.Catalog.Grains;

/// <summary>
/// One grain per voucher code (the grain key is the normalized code itself), so concurrent
/// redemption attempts against the same code are serialized by Orleans' single-activation
/// guarantee instead of needing manual locks or relying on a DB-level race to fail safely.
/// </summary>
public interface IVoucherGrain : IGrainWithStringKey
{
    Task<VoucherCreateResult> CreateAsync(VoucherCreateSpec spec, CancellationToken ct);
    Task<VoucherRedeemResult> RedeemAsync(PlayerId playerId, CancellationToken ct);
    Task<VoucherCreateResult> DeactivateAsync(CancellationToken ct);
    Task<VoucherSnapshot> GetSnapshotAsync(CancellationToken ct);
}
