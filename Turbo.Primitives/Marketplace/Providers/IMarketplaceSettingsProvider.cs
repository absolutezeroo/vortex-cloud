using System.Threading;
using System.Threading.Tasks;
using Turbo.Primitives.Marketplace.Snapshots;

namespace Turbo.Primitives.Marketplace.Providers;

public interface IMarketplaceSettingsProvider
{
    public MarketplaceSettingsSnapshot GetSettings();

    public Task ReloadAsync(CancellationToken ct);
}
