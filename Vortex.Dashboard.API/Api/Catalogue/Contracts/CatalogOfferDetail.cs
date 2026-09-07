using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// One offer and everything in its bundle.
/// </summary>
/// <param name="Id">The same id as <c>Offer.Id</c>. Kept because it was on the wire before this
/// record existed and the page reads the envelope, not only the offer inside it.</param>
public sealed record CatalogOfferDetail(
    int Id,
    CatalogOfferView Offer,
    IReadOnlyList<CatalogProductRow> Products
);
