using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Primitives.Catalog.Snapshots;
using Turbo.Primitives.Players;

namespace Turbo.Primitives.Catalog.Grains;

public partial interface ILtdRaffleGrain : IGrainWithIntegerKey
{
    Task<LtdRaffleEntryResult> EnterRaffleAsync(PlayerId playerId, CancellationToken ct);
    Task<UpcomingLtdSnapshot?> GetUpcomingLtdAsync(CancellationToken ct);
    Task<LtdSeriesSnapshot?> GetSeriesSnapshotAsync(CancellationToken ct);
    Task ForceRunRaffleAsync(CancellationToken ct);
    Task ReloadSeriesAsync(CancellationToken ct);
}
