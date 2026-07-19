using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Orleans.Snapshots.Players;

namespace Vortex.Primitives.Players.Grains;

public partial interface IPlayerPresenceGrain
{
    public Task OnPlayerUpdatedAsync(PlayerSummarySnapshot snapshot, CancellationToken ct);
    public Task OnFigureUpdatedAsync(PlayerSummarySnapshot snapshot, CancellationToken ct);
}
