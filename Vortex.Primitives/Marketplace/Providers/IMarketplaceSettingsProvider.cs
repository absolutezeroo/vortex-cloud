using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Marketplace.Snapshots;

namespace Vortex.Primitives.Marketplace.Providers;

public interface IMarketplaceSettingsProvider
{
    public MarketplaceSettingsSnapshot GetSettings();

    public Task ReloadAsync(CancellationToken ct);
}
