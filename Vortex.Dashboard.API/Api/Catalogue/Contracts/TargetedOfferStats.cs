using System.Collections.Generic;
using Vortex.Dashboard.API.Api.Hotel.Contracts;

namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>What the targeted offers sold over the window, from the audit trail.</summary>
public sealed record TargetedOfferStats(
    ReportWindow Window,
    TargetedOfferTotals Totals,
    IReadOnlyList<TargetedOfferPoint> Timeline,
    IReadOnlyList<TargetedOfferSales> TopOffers
);
