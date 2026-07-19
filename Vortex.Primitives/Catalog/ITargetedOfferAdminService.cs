using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Catalog.Admin;

namespace Vortex.Primitives.Catalog;

/// <summary>
/// CRUD for targeted_offers/targeted_offer_products, used by the dashboard's targeted-offer admin
/// surface. Every write reloads the <see cref="Grains.ITargetedOfferManagerGrain"/> definition cache
/// so the live offers players see never drift from the database — see the implementation.
/// </summary>
public interface ITargetedOfferAdminService
{
    Task<CatalogAdminResult> CreateOfferAsync(TargetedOfferCreateSpec spec, CancellationToken ct);
    Task<CatalogAdminResult> UpdateOfferAsync(
        int offerId,
        TargetedOfferUpdateSpec spec,
        CancellationToken ct
    );
    Task<CatalogAdminResult> DeleteOfferAsync(int offerId, CancellationToken ct);

    Task<CatalogAdminResult> CreateProductAsync(
        TargetedOfferProductCreateSpec spec,
        CancellationToken ct
    );
    Task<CatalogAdminResult> UpdateProductAsync(
        int productId,
        TargetedOfferProductUpdateSpec spec,
        CancellationToken ct
    );
    Task<CatalogAdminResult> DeleteProductAsync(int productId, CancellationToken ct);
}
