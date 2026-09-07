using System.Collections.Generic;
using Vortex.Dashboard.API.Api.Hotel.Contracts;

namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>What the catalogue sold, and what it earned.</summary>
public sealed record CatalogPurchaseStats(
    ReportWindow Window,
    CatalogPurchaseTotals Totals,
    IReadOnlyList<CatalogPurchasePoint> Timeline,
    IReadOnlyList<CatalogOfferSales> TopOffers
);
