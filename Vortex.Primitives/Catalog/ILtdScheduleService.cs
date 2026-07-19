using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Primitives.Catalog;

public interface ILtdScheduleService
{
    Task<LimitedOfferAppearanceSnapshot> GetNextAppearanceAsync(CancellationToken ct = default);

    /// <summary>The catalog page whose limited-edition offer expires soonest, for the landing
    /// page's "expiring soon" promo. Empty PageName if nothing is currently active.</summary>
    Task<CatalogPageExpirySnapshot> GetPageWithEarliestExpiryAsync(CancellationToken ct = default);
}
