using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Players.Snapshots;

namespace Vortex.Primitives.Players.Grains;

public partial interface IPlayerPresenceGrain
{
    public Task OnCurrencyUpdateAsync(WalletCurrencyUpdateSnapshot snapshot, CancellationToken ct);
}
