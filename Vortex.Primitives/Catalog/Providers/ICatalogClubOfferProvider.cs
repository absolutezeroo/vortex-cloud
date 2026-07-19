using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Primitives.Catalog.Providers;

public interface ICatalogClubOfferProvider
{
    IReadOnlyList<ClubOffer> GetAll();
    ClubOffer? FindById(int offerId);
    Task ReloadAsync(CancellationToken ct);
}
